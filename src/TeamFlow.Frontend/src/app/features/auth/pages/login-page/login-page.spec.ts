import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthStore } from '../../../../core/auth/auth.store';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  const login = vi.fn();

  function create() {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const set = (selector: string, value: string) => {
      const input = el.querySelector<HTMLInputElement>(selector)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    const submit = () => {
      el.querySelector('form')!.dispatchEvent(new Event('submit'));
      fixture.detectChanges();
    };
    return { el, set, submit };
  }

  beforeEach(() => {
    login.mockReset();
    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([]), { provide: AuthStore, useValue: { login } }],
    });
  });

  it('does not submit an empty form and shows required errors', () => {
    const { el, submit } = create();
    submit();
    expect(login).not.toHaveBeenCalled();
    expect(el.textContent).toContain('Email is required.');
    expect(el.textContent).toContain('Password is required.');
  });

  it('signs in and navigates to /projects', () => {
    login.mockReturnValue(of(undefined));
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const { set, submit } = create();
    set('input[type=email]', 'anna@example.com');
    set('input[formControlName=password]', 'secret');
    submit();
    expect(login).toHaveBeenCalledWith({ email: 'anna@example.com', password: 'secret' });
    expect(navigate).toHaveBeenCalledWith('/projects');
  });

  it('shows an invalid-credentials message on 401', () => {
    login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    const { el, set, submit } = create();
    set('input[type=email]', 'anna@example.com');
    set('input[formControlName=password]', 'wrong');
    submit();
    expect(el.querySelector('[role=alert]')?.textContent).toContain(
      'The email or password is incorrect.',
    );
  });
});
