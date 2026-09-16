import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { USER_ROLES, UserRole } from '../../../../core/auth/auth.models';
import { AuthStore } from '../../../../core/auth/auth.store';
import { isApiValidationProblemDetails } from '../../../../core/http/api-problem.models';
import { applyServerValidationErrors } from '../../../../core/http/server-validation';

const containsUppercase: ValidatorFn = (control): ValidationErrors | null =>
  /[A-Z]/.test(String(control.value)) ? null : { uppercase: true };
const containsDigit: ValidatorFn = (control): ValidationErrors | null =>
  /\d/.test(String(control.value)) ? null : { digit: true };
const containsSpecialCharacter: ValidatorFn = (control): ValidationErrors | null =>
  /[^a-zA-Z0-9]/.test(String(control.value)) ? null : { specialCharacter: true };
const isAllowedRole: ValidatorFn = (control): ValidationErrors | null =>
  USER_ROLES.some((role) => role === control.value) ? null : { role: true };

@Component({
  selector: 'app-register-page',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './register-page.html',
})
export class RegisterPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly roles = USER_ROLES;
  protected readonly form = this.formBuilder.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(128),
        containsUppercase,
        containsDigit,
        containsSpecialCharacter,
      ],
    ],
    role: this.formBuilder.nonNullable.control<UserRole>('Developer', [
      Validators.required,
      isAllowedRole,
    ]),
  });
  protected readonly isSubmitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly summaryErrors = signal<string[]>([]);
  protected readonly showPassword = signal(false);

  protected submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.formError.set(null);
    this.summaryErrors.set([]);
    this.isSubmitting.set(true);

    this.auth
      .register(this.form.getRawValue())
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => void this.router.navigate(['/projects']),
        error: (error: unknown) => this.handleError(error),
      });
  }

  protected fieldError(control: AbstractControl, label: string): string {
    const serverErrors = this.serverErrors(control);
    if (serverErrors.length) return serverErrors.join(' ');
    if (control.hasError('required')) return `${label} is required.`;
    if (control.hasError('maxlength')) return `${label} is too long.`;
    return `Enter a valid ${label.toLowerCase()}.`;
  }

  protected passwordError(): string {
    const control = this.form.controls.password;
    const serverErrors = this.serverErrors(control);
    if (serverErrors.length) return serverErrors.join(' ');
    if (control.hasError('required')) return 'Password is required.';
    if (control.hasError('minlength')) return 'Password must contain at least 8 characters.';
    if (control.hasError('maxlength')) return 'Password cannot exceed 128 characters.';
    if (control.hasError('uppercase')) return 'Password must contain an uppercase letter.';
    if (control.hasError('digit')) return 'Password must contain a digit.';
    return 'Password must contain a special character.';
  }

  private handleError(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) {
      this.formError.set('Unable to create the account. Please try again.');
      return;
    }

    if (error.status === 409) {
      const email = this.form.controls.email;
      email.setErrors({ ...email.errors, server: ['An account with this email already exists.'] });
      email.markAsTouched();
    } else if (error.status === 422 && isApiValidationProblemDetails(error.error)) {
      this.summaryErrors.set(applyServerValidationErrors(this.form, error.error));
    } else if (error.status === 429) {
      this.formError.set('Too many registration attempts. Please wait and try again.');
    } else {
      this.formError.set('Unable to create the account. Please try again.');
    }
  }

  private serverErrors(control: AbstractControl): string[] {
    const errors: unknown = control.getError('server');
    return Array.isArray(errors)
      ? errors.filter((message): message is string => typeof message === 'string')
      : [];
  }
}
