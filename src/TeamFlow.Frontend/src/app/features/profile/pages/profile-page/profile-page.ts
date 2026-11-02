import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { AuthStore } from '../../../../core/auth/auth.store';
import { isApiValidationProblemDetails } from '../../../../core/http/api-problem.models';
import { applyServerValidationErrors } from '../../../../core/http/server-validation';

@Component({
  selector: 'app-profile-page',
  imports: [
    DatePipe,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
  ],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
})
export class ProfilePage {
  protected readonly auth = inject(AuthStore);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    firstName: [
      this.auth.profile()?.firstName ?? '',
      [Validators.required, Validators.maxLength(100)],
    ],
    lastName: [
      this.auth.profile()?.lastName ?? '',
      [Validators.required, Validators.maxLength(100)],
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

    this.auth
      .updateProfile(this.form.getRawValue())
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => this.snackBar.open('Profile saved', 'Close', { duration: 4000 }),
        error: (error: unknown) => this.handleError(error),
      });
  }

  protected error(control: 'firstName' | 'lastName', label: string): string {
    const server: unknown = this.form.controls[control].getError('server');
    if (Array.isArray(server) && server.length) return server.join(' ');
    return this.form.controls[control].hasError('required')
      ? `${label} is required.`
      : `${label} is too long.`;
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

    this.formError.set('Unable to save your profile. Please try again.');
  }
}
