import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthStore } from '../../../../core/auth/auth.store';
import { isApiValidationProblemDetails } from '../../../../core/http/api-problem.models';
import { applyServerValidationErrors } from '../../../../core/http/server-validation';

@Component({
  selector: 'app-login-page',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly auth = inject(AuthStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  protected readonly isSubmitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly summaryErrors = signal<string[]>([]);
  protected readonly showPassword = signal(false);

  protected get email(): AbstractControl {
    return this.form.controls.email;
  }

  protected get password(): AbstractControl {
    return this.form.controls.password;
  }

  protected submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.formError.set(null);
    this.summaryErrors.set([]);
    this.isSubmitting.set(true);

    this.auth
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl(this.returnUrl),
        error: (error: unknown) => this.handleError(error),
      });
  }

  protected emailError(): string {
    const serverErrors = this.serverErrors(this.email);
    if (serverErrors.length) return serverErrors.join(' ');
    if (this.email.hasError('required')) return 'Email is required.';
    return 'Enter a valid email address.';
  }

  protected passwordError(): string {
    const serverErrors = this.serverErrors(this.password);
    return serverErrors.length ? serverErrors.join(' ') : 'Password is required.';
  }

  private get returnUrl(): string {
    const requested = this.route.snapshot.queryParamMap.get('returnUrl');

    if (
      requested &&
      requested.startsWith('/') &&
      !requested.startsWith('//') &&
      !requested.startsWith('/login') &&
      !requested.startsWith('/register')
    ) {
      return requested;
    }

    return '/projects';
  }

  private handleError(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) {
      this.formError.set('Unable to sign in. Please try again.');
      return;
    }

    if (error.status === 401) {
      this.formError.set('The email or password is incorrect.');
    } else if (error.status === 422 && isApiValidationProblemDetails(error.error)) {
      this.summaryErrors.set(applyServerValidationErrors(this.form, error.error));
    } else if (error.status === 429) {
      this.formError.set('Too many sign-in attempts. Please wait and try again.');
    } else {
      this.formError.set('Unable to sign in. Please try again.');
    }
  }

  private serverErrors(control: AbstractControl): string[] {
    const errors: unknown = control.getError('server');
    return Array.isArray(errors)
      ? errors.filter((message): message is string => typeof message === 'string')
      : [];
  }
}
