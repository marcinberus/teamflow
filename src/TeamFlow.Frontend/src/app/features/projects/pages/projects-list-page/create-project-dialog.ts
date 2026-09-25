import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { isApiValidationProblemDetails } from '../../../../core/http/api-problem.models';
import { applyServerValidationErrors } from '../../../../core/http/server-validation';
import { ProjectsApi } from '../../projects.api';

@Component({
  selector: 'app-create-project-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
  ],
  templateUrl: './create-project-dialog.html',
  styleUrl: './create-project-dialog.scss',
})
export class CreateProjectDialog {
  private readonly api = inject(ProjectsApi);
  private readonly dialogRef = inject(MatDialogRef<CreateProjectDialog>);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(2000)],
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
    this.api
      .createProject(this.form.getRawValue())
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: ({ projectId }) => this.dialogRef.close(projectId),
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
    if (
      error instanceof HttpErrorResponse &&
      error.status === 422 &&
      isApiValidationProblemDetails(error.error)
    ) {
      this.summaryErrors.set(applyServerValidationErrors(this.form, error.error));
      return;
    }

    this.formError.set('Unable to create the project. Please try again.');
  }
}
