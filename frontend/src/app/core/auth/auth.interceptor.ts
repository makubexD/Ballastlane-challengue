import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const token = authService.token();

  if (token !== null) {
    const authenticatedRequest = request.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
    return next(authenticatedRequest);
  }

  return next(request);
};
