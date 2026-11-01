import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { App } from './app';
import { routes } from './app.routes';
import { AuthStore } from './core/auth/auth.store';

describe('App', () => {
  const auth = {
    bootstrap: vi.fn(() => of(true)),
    displayName: signal('Test User'),
    logout: vi.fn(),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    auth.bootstrap.mockReturnValue(of(true));

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes), { provide: AuthStore, useValue: auth }],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('renders public authentication routes without the application shell', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/login');
    await fixture.whenStable();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-shell')).toBeNull();
    expect(compiled.textContent).toContain('Sign in to TeamFlow');
  });

  it('renders workspace routes inside the application shell', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/projects');
    await fixture.whenStable();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-shell')).toBeTruthy();
    expect(compiled.textContent).toContain('Browse and manage your team’s work.');
  });

  it('redirects an anonymous workspace navigation to login', async () => {
    auth.bootstrap.mockReturnValueOnce(of(false));
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/projects');
    await fixture.whenStable();
    fixture.detectChanges();

    expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fprojects');
    expect((fixture.nativeElement as HTMLElement).querySelector('app-shell')).toBeNull();
  });
});
