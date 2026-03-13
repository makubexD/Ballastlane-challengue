import { HttpInterceptorFn } from '@angular/common/http';

const MUTATING_METHODS = ['POST', 'PUT', 'DELETE', 'PATCH'];

export const csrfInterceptor: HttpInterceptorFn = (req, next) => {
  if (!MUTATING_METHODS.includes(req.method)) {
    return next(req);
  }
  const token = getCsrfToken();
  if (!token) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { 'X-XSRF-TOKEN': token } }));
};

function getCsrfToken(): string | null {
  const match = document.cookie.match(/(?:^|;\s*)XSRF-TOKEN=([^;]+)/);
  return match ? decodeURIComponent(match[1]) : null;
}
