import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import {
  catchError,
  finalize,
  map,
  Observable,
  of,
  shareReplay,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { AuthApi } from './auth.api';
import { LoginRequest, RegisterRequest, UserProfile } from './auth.models';
import { TokenStorage } from './token-storage';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);
  private readonly tokenStorage = inject(TokenStorage);
  private readonly tokenState = signal<string | null>(this.tokenStorage.get());
  private readonly profileState = signal<UserProfile | null>(null);
  private readonly loadingProfileState = signal(false);
  private bootstrapRequest: Observable<boolean> | null = null;

  readonly token = this.tokenState.asReadonly();
  readonly profile = this.profileState.asReadonly();
  readonly isLoadingProfile = this.loadingProfileState.asReadonly();
  readonly isAuthenticated = computed(
    () => this.tokenState() !== null && this.profileState() !== null,
  );
  readonly userId = computed(() => this.profileState()?.id ?? null);
  readonly role = computed(() => this.profileState()?.role ?? null);
  readonly displayName = computed(() => {
    const profile = this.profileState();
    return profile ? `${profile.firstName} ${profile.lastName}`.trim() : '';
  });

  login(request: LoginRequest): Observable<UserProfile> {
    return this.api.login(request).pipe(switchMap(({ token }) => this.establishSession(token)));
  }

  register(request: RegisterRequest): Observable<UserProfile> {
    return this.api.register(request).pipe(switchMap(({ token }) => this.establishSession(token)));
  }

  bootstrap(): Observable<boolean> {
    if (this.isAuthenticated()) {
      return of(true);
    }

    if (!this.tokenState()) {
      return of(false);
    }

    if (this.bootstrapRequest) {
      return this.bootstrapRequest;
    }

    this.loadingProfileState.set(true);
    this.bootstrapRequest = this.api.getProfile().pipe(
      tap((profile) => this.profileState.set(profile)),
      map(() => true),
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 404)) {
          this.clearInvalidSession();
        }

        return of(false);
      }),
      finalize(() => {
        this.loadingProfileState.set(false);
        this.bootstrapRequest = null;
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    return this.bootstrapRequest;
  }

  logout(): void {
    this.clearSession();
  }

  clearInvalidSession(): void {
    this.clearSession();
  }

  private establishSession(token: string): Observable<UserProfile> {
    this.tokenStorage.set(token);
    this.tokenState.set(token);
    this.profileState.set(null);
    this.loadingProfileState.set(true);

    return this.api.getProfile().pipe(
      tap((profile) => this.profileState.set(profile)),
      catchError((error: unknown) => {
        this.clearSession();
        return throwError(() => error);
      }),
      finalize(() => this.loadingProfileState.set(false)),
    );
  }

  private clearSession(): void {
    this.tokenStorage.clear();
    this.tokenState.set(null);
    this.profileState.set(null);
  }
}
