import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // 401 is mostly handled by authInterceptor (refresh token flow)
      // If authInterceptor's refresh token fails, it will logout and throw an error which will be caught here (or there)
      if (error.status === 401 && !req.url.includes('/refresh-token')) {
        // We can let authInterceptor handle the retry, but if we end up here
        // and we haven't handled it, it means the token is completely invalid.
        // auth.interceptor already triggers logout on refresh failure.
      } else if (error.status === 403) {
        // Forbidden
        router.navigate(['/unauthorized']); // We'll need an unauthorized route eventually
      } else if (error.status === 500 || error.status === 0) {
        // Server Error or Network Error
        // We can show a toast here. For now we will just log it.
        console.error('An unexpected error occurred. Please try again later.');
      }

      return throwError(() => error);
    })
  );
};
