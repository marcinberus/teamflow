import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { firstValueFrom, Observable, of } from 'rxjs';
import { AuthStore } from './auth.store';
import { roleGuard } from './role.guard';

describe('roleGuard', () => {
  const auth = {
    bootstrap: vi.fn(() => of(true)),
    role: signal<'Admin' | 'Manager' | 'Developer' | null>('Manager'),
  };
  const projectsTree = {} as UrlTree;
  const router = {
    createUrlTree: vi.fn(() => projectsTree),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    auth.bootstrap.mockReturnValue(of(true));
    auth.role.set('Manager');
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthStore, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });
  });

  it('allows configured roles and redirects other authenticated roles', async () => {
    const route = { data: { roles: ['Admin', 'Manager'] } } as unknown as ActivatedRouteSnapshot;
    const state = { url: '/admin' } as RouterStateSnapshot;

    const allowed = TestBed.runInInjectionContext(() => roleGuard(route, state));
    expect(await firstValueFrom(allowed as Observable<boolean | UrlTree>)).toBe(true);

    auth.role.set('Developer');
    const denied = TestBed.runInInjectionContext(() => roleGuard(route, state));
    expect(await firstValueFrom(denied as Observable<boolean | UrlTree>)).toBe(projectsTree);
    expect(router.createUrlTree).toHaveBeenLastCalledWith(['/projects']);
  });
});
