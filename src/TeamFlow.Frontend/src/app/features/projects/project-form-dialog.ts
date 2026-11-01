import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize, map } from 'rxjs';
import { isApiValidationProblemDetails } from '../../core/http/api-problem.models';
import { applyServerValidationErrors } from '../../core/http/server-validation';
import { ProjectsApi } from './projects.api';
import { ProjectDetails } from './projects.models';

/** Creates a project, or edits one when opened with `data: project`. Closes with the project id. */
@Component({
  selector: 'app-project-form-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
  ],
  templateUrl: './project-form-dialog.html',
  styleUrl: './project-form-dialog.scss',
})
export class ProjectFormDialog {
  private readonly api = inject(ProjectsApi);
  private readonly dialogRef = inject(MatDialogRef<ProjectFormDialog>);
  private readonly formBuilder = inject(FormBuilder);
  protected readonly project = inject<ProjectDetails | null>(MAT_DIALOG_DATA, { optional: true });

  protected readonly form = this.formBuilder.nonNullable.group({
    name: [this.project?.name ?? '', [Validators.required, Validators.maxLength(200)]],
    description: [this.project?.description ?? '', Validators.maxLength(2000)],
  });
  protected readonly isSubmitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly summaryErrors = signal<string[]>([]);

  protected get name() {
    return this.form.controls.name;
  }

  protected get description() {
    return this.form.controls.description;
  }

  protected submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.formError.set(null);
    this.summaryErrors.set([]);
    this.isSubmitting.set(true);

    const request = this.form.getRawValue();
    const project = this.project;
    const projectId$ = project
      ? this.api.updateProject(project.id, request).pipe(map(() => project.id))
      : this.api.createProject(request).pipe(map(({ projectId }) => projectId));

    projectId$.pipe(finalize(() => this.isSubmitting.set(false))).subscribe({
      next: (projectId) => this.dialogRef.close(projectId),
      error: (error: unknown) => this.handleError(error),
    });
  }

  protected close(): void {
    this.dialogRef.close();
  }

  protected nameError(): string {
    return this.fieldError(this.name, 'Name');
  }

  protected descriptionError(): string {
    return this.fieldError(this.description, 'Description');
  }

  private fieldError(control: typeof this.name, label: string): string {
    const serverErrors: unknown = control.getError('server');
    if (Array.isArray(serverErrors) && serverErrors.length) return serverErrors.join(' ');
    if (control.hasError('required')) return `${label} is required.`;
    return `${label} is too long.`;
  }

  private handleError(error: unknown): void {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 422 && isApiValidationProblemDetails(error.error)) {
        this.summaryErrors.set(applyServerValidationErrors(this.form, error.error));
        return;
      }
      if (error.status === 403) {
        this.formError.set('You do not have permission to change this project.');
        return;
      }
      if (error.status === 404 && this.project) {
        this.formError.set('This project no longer exists.');
        return;
      }
    }

    this.formError.set(
      `Unable to ${this.project ? 'save' : 'create'} the project. Please try again.`,
    );
  }
}
