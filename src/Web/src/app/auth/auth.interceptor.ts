import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const isApi = request.url.startsWith('/api/contacts/') || request.url.startsWith('/api/sales/');
  const isLogin = request.url === '/api/contacts/v1/auth/login';
  const token = isApi && !isLogin ? auth.token() : null;
  const authorized = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

  return next(authorized).pipe(catchError((error: unknown) => {
    if (error instanceof HttpErrorResponse && error.status === 401 && isApi && !isLogin) {
      auth.clear();
      void router.navigateByUrl('/login');
    }
    return throwError(() => error);
  }));
};
