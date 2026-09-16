import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { UserRole } from './auth.models';
import { AuthStore } from './auth.store';

export const roleGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  const allowedRoles = route.data['roles'] as readonly UserRole[] | undefined;

  if (!allowedRoles?.length) {
    return true;
  }

  return auth.bootstrap().pipe(
    map((isAuthenticated) => {
      if (!isAuthenticated) {
        return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
      }

      const role = auth.role();
      return role !== null && allowedRoles.includes(role)
        ? true
        : router.createUrlTree(['/projects']);
    }),
  );
};
