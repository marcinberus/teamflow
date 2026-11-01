import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, map } from 'rxjs';
import { isApiValidationProblemDetails } from '../../../../core/http/api-problem.models';
import { applyServerValidationErrors } from '../../../../core/http/server-validation';
import { AssigneeOption } from '../../project.policies';
import { TaskItem } from '../../projects.models';
import { TasksApi } from '../../tasks.api';

export interface TaskFormDialogData {
  projectId: string;
  assignees: readonly AssigneeOption[];
  /** Present when editing. */
  task?: TaskItem;
}

/** `YYYY-MM-DD` in the user's local time zone. */
const localDate = (date: Date) => date.toLocaleDateString('sv');

/** Creates a task, or edits one when `data.task` is set. Closes with `true` on success. */
@Component({
  selector: 'app-task-form-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ task ? 'Edit task' : 'New task' }}</h2>
    <form [formGroup]="form" novalidate (ngSubmit)="submit()">
      <mat-dialog-content>
        @if (formError()) {
          <div class="form-alert" role="alert">{{ formError() }}</div>
        }
        @if (summaryErrors().length) {
          <div class="form-error-summary" role="alert">
            <p>Check the following:</p>
            <ul>
              @for (message of summaryErrors(); track $index) {
                <li>{{ message }}</li>
              }
            </ul>
          </div>
        }
        <mat-form-field appearance="outline">
          <mat-label>Title</mat-label>
          <input matInput formControlName="title" maxlength="300" cdkFocusInitial />
          @if (form.controls.title.invalid && form.controls.title.touched) {
            <mat-error>{{ error(form.controls.title, 'Title') }}</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Description</mat-label>
          <textarea matInput formControlName="description" rows="4" maxlength="2000"></textarea>
          <mat-hint align="end">{{ form.controls.description.value.length }}/2000</mat-hint>
          @if (form.controls.description.invalid && form.controls.description.touched) {
            <mat-error>{{ error(form.controls.description, 'Description') }}</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Assignee</mat-label>
          <mat-select formControlName="assignedUserId">
            <mat-option [value]="null">{{ task ? 'Unassigned' : 'Me (default)' }}</mat-option>
            @for (assignee of data.assignees; track assignee.userId) {
              <mat-option [value]="assignee.userId">{{ assignee.name }}</mat-option>
            }
          </mat-select>
          @if (form.controls.assignedUserId.invalid) {
            <mat-error>{{ error(form.controls.assignedUserId, 'Assignee') }}</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Due date</mat-label>
          <input matInput type="date" formControlName="dueDate" />
          @if (form.controls.dueDate.invalid && form.controls.dueDate.touched) {
            <mat-error>{{ error(form.controls.dueDate, 'Due date') }}</mat-error>
          }
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" [disabled]="isSubmitting()" (click)="dialogRef.close()">
          Cancel
        </button>
        <button mat-flat-button type="submit" [disabled]="isSubmitting()">
          @if (isSubmitting()) {
            <mat-spinner [diameter]="18" /> Saving…
          } @else {
            {{ task ? 'Save' : 'Create task' }}
          }
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styleUrl: '../../project-form-dialog.scss',
})
export class TaskFormDialog {
  protected readonly dialogRef = inject(MatDialogRef<TaskFormDialog>);
  protected readonly data = inject<TaskFormDialogData>(MAT_DIALOG_DATA);
  private readonly api = inject(TasksApi);

  protected readonly task = this.data.task;
  private readonly initialDueDate = this.task?.dueDate
    ? localDate(new Date(this.task.dueDate))
    : '';

  protected readonly form = inject(FormBuilder).nonNullable.group({
    title: [this.task?.title ?? '', [Validators.required, Validators.maxLength(300)]],
    description: [this.task?.description ?? '', Validators.maxLength(2000)],
    assignedUserId: [this.task?.assignedUserId ?? (null as string | null)],
    dueDate: [
      this.initialDueDate,
      // must be in the future; an untouched existing due date is sent back as-is
      (c: AbstractControl<string>) =>
        c.value && c.value !== this.initialDueDate && c.value < localDate(new Date())
          ? { past: true }
          : null,
    ],
  });
  protected readonly isSubmitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly summaryErrors = signal<string[]>([]);

  protected submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.formError.set(null);
    this.summaryErrors.set([]);
    this.isSubmitting.set(true);

    const { title, description, assignedUserId, dueDate } = this.form.getRawValue();
    const body = {
      title: title.trim(),
      description,
      assignedUserId,
      // end of the chosen local day, so "today" is still in the future
      dueDate:
        dueDate === this.initialDueDate
          ? (this.task?.dueDate ?? null)
          : dueDate
            ? new Date(`${dueDate}T23:59:59`).toISOString()
            : null,
    };
    const { projectId } = this.data;
    const request$ = this.task
      ? this.api.updateTask(projectId, this.task.id, body)
      : this.api.createTask(projectId, body).pipe(map(() => undefined));

    request$.pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: () => this.dialogRef.close(true),
      error: (error: unknown) => this.handleError(error),
    });
  }

  protected error(control: AbstractControl, label: string): string {
    const serverErrors: unknown = control.getError('server');
    if (Array.isArray(serverErrors) && serverErrors.length) return serverErrors.join(' ');
    if (control.hasError('required')) return `${label} is required.`;
    if (control.hasError('past')) return `${label} must be today or later.`;
    return `${label} is too long.`;
  }

  private handleError(error: unknown): void {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 422 && isApiValidationProblemDetails(error.error)) {
        this.summaryErrors.set(applyServerValidationErrors(this.form, error.error));
        return;
      }
      if (error.status === 403) {
        this.formError.set('You must be a member of this project to change its tasks.');
        return;
      }
      if (error.status === 404) {
        this.formError.set('The project or the task no longer exists.');
        return;
      }
    }

    this.formError.set(`Unable to ${this.task ? 'save' : 'create'} the task. Please try again.`);
  }
}
