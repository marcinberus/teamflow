import { FormGroup } from '@angular/forms';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthStore } from '../../../../core/auth/auth.store';
import { RegisterPage } from './register-page';

describe('RegisterPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterPage],
      providers: [provideRouter([]), { provide: AuthStore, useValue: { register: vi.fn() } }],
    }).compileComponents();
  });

  it('mirrors every backend password requirement', () => {
    const fixture = TestBed.createComponent(RegisterPage);
    const form = (fixture.componentInstance as unknown as { form: FormGroup }).form;
    const password = form.controls['password'];

    password.setValue('short');
    expect(password.hasError('minlength')).toBe(true);
    expect(password.hasError('uppercase')).toBe(true);
    expect(password.hasError('digit')).toBe(true);
    expect(password.hasError('specialCharacter')).toBe(true);

    password.setValue('ValidPassword1!');
    expect(password.valid).toBe(true);

    password.setValue(`A1!${'a'.repeat(126)}`);
    expect(password.hasError('maxlength')).toBe(true);
  });

  it('accepts only backend-defined roles', () => {
    const fixture = TestBed.createComponent(RegisterPage);
    const form = (fixture.componentInstance as unknown as { form: FormGroup }).form;
    const role = form.controls['role'];

    role.setValue('Owner');
    expect(role.hasError('role')).toBe(true);

    role.setValue('Developer');
    expect(role.valid).toBe(true);
  });
});
