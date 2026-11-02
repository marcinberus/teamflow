import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { AuthApi } from './auth.api';
import { UserProfile } from './auth.models';
import { AuthStore } from './auth.store';
import { TokenStorage } from './token-storage';

describe('AuthStore', () => {
  const profile: UserProfile = {
    id: 'user-1',
    email: 'anna@example.com',
    firstName: 'Anna',
    lastName: 'Kowal',
    role: 'Manager',
    createdAt: '2026-09-16T08:00:00Z',
  };
  const api = {
    login: vi.fn(),
    register: vi.fn(),
    getProfile: vi.fn(),
    updateProfile: vi.fn(),
  };
  const tokenStorage = {
    get: vi.fn<() => string | null>(() => null),
    set: vi.fn(),
    clear: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    tokenStorage.get.mockReturnValue(null);
    api.getProfile.mockReturnValue(of(profile));
    TestBed.configureTestingModule({
      providers: [
        AuthStore,
        { provide: AuthApi, useValue: api },
        { provide: TokenStorage, useValue: tokenStorage },
      ],
    });
  });

  it('hydrates a persisted session from the canonical profile endpoint', async () => {
    tokenStorage.get.mockReturnValue('persisted-token');
    const store = TestBed.inject(AuthStore);

    expect(await firstValueFrom(store.bootstrap())).toBe(true);
    expect(api.getProfile).toHaveBeenCalledOnce();
    expect(store.isAuthenticated()).toBe(true);
    expect(store.userId()).toBe('user-1');
    expect(store.role()).toBe('Manager');
    expect(store.displayName()).toBe('Anna Kowal');
  });

  it('stores a login token and then loads the canonical profile', async () => {
    api.login.mockReturnValue(of({ token: 'new-token', userId: 'user-1', role: 'Manager' }));
    const store = TestBed.inject(AuthStore);

    const result = await firstValueFrom(
      store.login({ email: 'anna@example.com', password: 'Password1!' }),
    );

    expect(tokenStorage.set).toHaveBeenCalledWith('new-token');
    expect(api.getProfile).toHaveBeenCalledOnce();
    expect(result).toEqual(profile);
    expect(store.profile()).toEqual(profile);
  });

  it('clears an invalid persisted session', async () => {
    tokenStorage.get.mockReturnValue('expired-token');
    api.getProfile.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' })),
    );
    const store = TestBed.inject(AuthStore);

    expect(await firstValueFrom(store.bootstrap())).toBe(false);
    expect(tokenStorage.clear).toHaveBeenCalledOnce();
    expect(store.token()).toBeNull();
    expect(store.profile()).toBeNull();
  });

  it('updates the local profile (and display name) after a saved name change', async () => {
    tokenStorage.get.mockReturnValue('persisted-token');
    api.updateProfile.mockReturnValue(of(undefined));
    const store = TestBed.inject(AuthStore);
    await firstValueFrom(store.bootstrap());

    await firstValueFrom(store.updateProfile({ firstName: 'Ola', lastName: 'Nowak' }));

    expect(api.updateProfile).toHaveBeenCalledWith({ firstName: 'Ola', lastName: 'Nowak' });
    expect(store.displayName()).toBe('Ola Nowak');
    expect(store.profile()?.email).toBe('anna@example.com');
  });

  it('removes all local session state on logout', async () => {
    api.login.mockReturnValue(of({ token: 'new-token', userId: 'user-1', role: 'Manager' }));
    const store = TestBed.inject(AuthStore);
    await firstValueFrom(store.login({ email: 'anna@example.com', password: 'Password1!' }));

    store.logout();

    expect(tokenStorage.clear).toHaveBeenCalledOnce();
    expect(store.isAuthenticated()).toBe(false);
    expect(store.token()).toBeNull();
    expect(store.profile()).toBeNull();
  });
});
