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
  template: `
    <header class="page-header">
      <h1>Profile</h1>
      <p>Your account details.</p>
    </header>

    @if (auth.profile(); as profile) {
      <section class="page-panel" aria-labelledby="account-title">
        <h2 id="account-title">Account</h2>
        <dl class="account">
          <div>
            <dt>Email</dt>
            <dd>{{ profile.email }}</dd>
          </div>
          <div>
            <dt>Role</dt>
            <dd>{{ profile.role }}</dd>
          </div>
          <div>
            <dt>Member since</dt>
            <dd>{{ profile.createdAt | date: 'mediumDate' }}</dd>
          </div>
        </dl>
      </section>

      <section class="page-panel" aria-labelledby="name-title">
        <h2 id="name-title">Name</h2>
        <form [formGroup]="form" novalidate (ngSubmit)="submit()">
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
            <mat-label>First name</mat-label>
            <input matInput formControlName="firstName" maxlength="100" autocomplete="given-name" />
            @if (form.controls.firstName.invalid && form.controls.firstName.touched) {
              <mat-error>{{ error('firstName', 'First name') }}</mat-error>
            }
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Last name</mat-label>
            <input matInput formControlName="lastName" maxlength="100" autocomplete="family-name" />
            @if (form.controls.lastName.invalid && form.controls.lastName.touched) {
              <mat-error>{{ error('lastName', 'Last name') }}</mat-error>
            }
          </mat-form-field>
          <button mat-flat-button type="submit" [disabled]="isSubmitting()">
            @if (isSubmitting()) {
              <mat-spinner [diameter]="18" /> Saving…
            } @else {
              Save
            }
          </button>
        </form>
      </section>
    }
  `,
  styles: `
    .account {
      display: grid;
      gap: 0.5rem;
      margin: 0;
    }
    .account div {
      display: flex;
      gap: 1rem;
    }
    .account dt {
      min-width: 8rem;
      opacity: 0.75;
    }
    .account dd {
      margin: 0;
    }
    form {
      display: grid;
      gap: 0.25rem;
      max-width: 30rem;
    }
    form button {
      justify-self: start;
    }
  `,
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
