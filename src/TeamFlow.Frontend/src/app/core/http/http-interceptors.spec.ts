import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { authInterceptor } from '../auth/auth.interceptor';
import { TokenStorage } from '../auth/token-storage';
import { errorInterceptor } from './error.interceptor';

describe('HTTP interceptors', () => {
  const tokenStorage = {
    get: vi.fn((): string | null => 'test-token'),
    set: vi.fn(),
    clear: vi.fn(),
  };
  const router = {
    url: '/projects',
    navigate: vi.fn(() => Promise.resolve(true)),
  };
  const snackBar = { open: vi.fn() };

  let http: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    vi.clearAllMocks();
    tokenStorage.get.mockReturnValue('test-token');
    router.url = '/projects';

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
        provideHttpClientTesting(),
        { provide: TokenStorage, useValue: tokenStorage },
        { provide: Router, useValue: router },
        { provide: MatSnackBar, useValue: snackBar },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('attaches the bearer token only to TeamFlow API requests', () => {
    http.get('/api/v1/projects').subscribe();
    const apiRequest = httpTesting.expectOne('/api/v1/projects');
    expect(apiRequest.request.headers.get('Authorization')).toBe('Bearer test-token');
    apiRequest.flush({});

    http.get('https://example.com/api/v1/projects').subscribe();
    const externalRequest = httpTesting.expectOne('https://example.com/api/v1/projects');
    expect(externalRequest.request.headers.has('Authorization')).toBe(false);
    externalRequest.flush({});
  });

  it('clears the session, redirects, and still returns a TeamFlow 401 to the caller', () => {
    let receivedError: HttpErrorResponse | undefined;
    http.get('/api/v1/projects').subscribe({
      error: (error: HttpErrorResponse) => (receivedError = error),
    });

    httpTesting
      .expectOne('/api/v1/projects')
      .flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(tokenStorage.clear).toHaveBeenCalledOnce();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/projects' },
    });
    expect(receivedError?.status).toBe(401);
  });

  it('leaves a 422 response available to feature code', () => {
    let receivedError: HttpErrorResponse | undefined;
    http.post('/api/v1/projects', {}).subscribe({
      error: (error: HttpErrorResponse) => (receivedError = error),
    });

    httpTesting
      .expectOne('/api/v1/projects')
      .flush(
        { title: 'Validation failed', errors: { Name: ['Name is required.'] } },
        { status: 422, statusText: 'Unprocessable Entity' },
      );

    expect(receivedError?.status).toBe(422);
    expect(tokenStorage.clear).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
    expect(snackBar.open).not.toHaveBeenCalled();
  });

  it('does not redirect again when a login request returns 401', () => {
    router.url = '/login';
    let receivedStatus: number | undefined;
    http.post('/api/v1/login', {}).subscribe({
      error: (error: HttpErrorResponse) => (receivedStatus = error.status),
    });

    httpTesting.expectOne('/api/v1/login').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(tokenStorage.clear).toHaveBeenCalledOnce();
    expect(router.navigate).not.toHaveBeenCalled();
    expect(receivedStatus).toBe(401);
  });

  it('shows retry-later feedback for a TeamFlow 429 and rethrows it', () => {
    let receivedStatus: number | undefined;
    http.get('/api/v1/projects').subscribe({
      error: (error: HttpErrorResponse) => (receivedStatus = error.status),
    });

    httpTesting
      .expectOne('/api/v1/projects')
      .flush({}, { status: 429, statusText: 'Too Many Requests' });

    expect(snackBar.open).toHaveBeenCalledWith(
      'Too many requests. Please wait and try again.',
      'Dismiss',
      { duration: 6000 },
    );
    expect(receivedStatus).toBe(429);
  });
});
