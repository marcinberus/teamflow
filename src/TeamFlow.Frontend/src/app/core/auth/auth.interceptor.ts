import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { isTeamFlowApiUrl } from '../config/api.config';
import { TokenStorage } from './token-storage';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(TokenStorage).get();

  if (!token || !isTeamFlowApiUrl(request.url)) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    }),
  );
};
