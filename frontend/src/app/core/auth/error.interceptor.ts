import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

const AUTH_BYPASS_URLS = ['/api/auth/login', '/api/auth/me'];

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        const isBypassUrl = AUTH_BYPASS_URLS.some(url => req.url.includes(url));
        if (!isBypassUrl) {
          authService.logout();
        }
      }
      return throwError(() => error);
    })
  );
};
