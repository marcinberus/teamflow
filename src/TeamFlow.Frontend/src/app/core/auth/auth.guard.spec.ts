import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { firstValueFrom, Observable, of } from 'rxjs';
import { authGuard } from './auth.guard';
import { AuthStore } from './auth.store';

describe('authGuard', () => {
  const bootstrap = vi.fn();

  function run(url: string) {
    TestBed.configureTestingModule({
      providers: [{ provide: AuthStore, useValue: { bootstrap } }],
    });
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );
    return firstValueFrom(result as Observable<boolean | UrlTree>);
  }

  it('allows an authenticated user', async () => {
    bootstrap.mockReturnValue(of(true));
    expect(await run('/projects')).toBe(true);
  });

  it('sends an anonymous user to login with the requested url', async () => {
    bootstrap.mockReturnValue(of(false));
    const result = await run('/projects/p1');
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe(
      '/login?returnUrl=%2Fprojects%2Fp1',
    );
  });
});
