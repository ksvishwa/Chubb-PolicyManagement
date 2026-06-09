import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError(err => {
      const message: string = err.error?.detail ?? err.error?.title ?? 'An unexpected error occurred.';
      console.error('[HTTP Error]', err.status, message);
      return throwError(() => new Error(message));
    })
  );
};
