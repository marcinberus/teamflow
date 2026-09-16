import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { TokenStorage } from '../auth/token-storage';
import { isTeamFlowApiUrl } from '../config/api.config';

const RETRY_LATER_MESSAGE = 'Too many requests. Please wait and try again.';
const INFRASTRUCTURE_ERROR_MESSAGE = 'TeamFlow is unavailable. Please try again.';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);
  const tokenStorage = inject(TokenStorage);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && isTeamFlowApiUrl(request.url)) {
        if (error.status === 401) {
          tokenStorage.clear();
          redirectToLogin(router);
        } else if (error.status === 429) {
          showMessage(snackBar, RETRY_LATER_MESSAGE);
        } else if (error.status === 0 || error.status >= 500) {
          showMessage(snackBar, INFRASTRUCTURE_ERROR_MESSAGE);
        }
      }

      return throwError(() => error);
    }),
  );
};

function redirectToLogin(router: Router): void {
  const isAuthRoute = router.url.startsWith('/login') || router.url.startsWith('/register');

  if (isAuthRoute) {
    return;
  }

  void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
}

function showMessage(snackBar: MatSnackBar, message: string): void {
  snackBar.open(message, 'Dismiss', { duration: 6000 });
}
