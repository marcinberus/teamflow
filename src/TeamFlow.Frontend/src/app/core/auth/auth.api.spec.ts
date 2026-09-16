import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthApi } from './auth.api';

describe('AuthApi', () => {
  let api: AuthApi;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthApi, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(AuthApi);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('uses the authentication API contracts', () => {
    const login = { email: 'anna@example.com', password: 'Password1!' };
    api.login(login).subscribe();
    const loginRequest = httpTesting.expectOne('/api/v1/login');
    expect(loginRequest.request.method).toBe('POST');
    expect(loginRequest.request.body).toEqual(login);
    loginRequest.flush({ token: 'token', userId: 'user-1', role: 'Manager' });

    const registration = {
      firstName: 'Anna',
      lastName: 'Kowal',
      email: 'anna@example.com',
      password: 'Password1!',
      role: 'Manager' as const,
    };
    api.register(registration).subscribe();
    const registerRequest = httpTesting.expectOne('/api/v1/users');
    expect(registerRequest.request.method).toBe('POST');
    expect(registerRequest.request.body).toEqual(registration);
    registerRequest.flush({ token: 'token', userId: 'user-1' });

    api.getProfile().subscribe();
    const profileRequest = httpTesting.expectOne('/api/v1/users/me');
    expect(profileRequest.request.method).toBe('GET');
    profileRequest.flush({
      id: 'user-1',
      email: 'anna@example.com',
      firstName: 'Anna',
      lastName: 'Kowal',
      role: 'Manager',
      createdAt: '2026-09-16T08:00:00Z',
    });
  });
});
