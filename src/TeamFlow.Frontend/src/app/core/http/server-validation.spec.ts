import { FormControl, FormGroup, Validators } from '@angular/forms';
import { applyServerValidationErrors } from './server-validation';

describe('applyServerValidationErrors', () => {
  it('maps every message, preserves client errors, and returns unmapped messages', () => {
    const form = new FormGroup({
      email: new FormControl('', { nonNullable: true, validators: Validators.required }),
      confirmPassword: new FormControl('', { nonNullable: true }),
    });

    const unmapped = applyServerValidationErrors(
      form,
      {
        errors: {
          Email: ['Email is invalid.', 'Email already exists.'],
          PasswordConfirmation: ['Passwords must match.'],
          Request: ['The submitted request is invalid.'],
        },
      },
      { PasswordConfirmation: 'confirmPassword' },
    );

    expect(form.controls.email.errors).toEqual({
      required: true,
      server: ['Email is invalid.', 'Email already exists.'],
    });
    expect(form.controls.confirmPassword.errors).toEqual({
      server: ['Passwords must match.'],
    });
    expect(form.controls.email.touched).toBe(true);
    expect(unmapped).toEqual(['The submitted request is invalid.']);
  });

  it('replaces stale server errors on the next response', () => {
    const form = new FormGroup({
      email: new FormControl('', { nonNullable: true }),
      firstName: new FormControl('', { nonNullable: true }),
    });

    applyServerValidationErrors(form, {
      errors: { Email: ['Old email error.'], FirstName: ['Old name error.'] },
    });
    applyServerValidationErrors(form, { errors: { Email: ['Current email error.'] } });

    expect(form.controls.email.errors).toEqual({ server: ['Current email error.'] });
    expect(form.controls.firstName.errors).toBeNull();
  });
});
