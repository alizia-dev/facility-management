import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

/**
 * Turns the API's RFC 9457 ProblemDetails responses into a single readable string,
 * and sends an expired session back to the login page.
 *
 * Note what a 404 means here. The API answers 404 — not 403 — when a caller asks
 * for another organisation's resource, so that status codes cannot be used to
 * probe for the existence of another tenant's data. The message below is
 * deliberately the same as for a genuinely missing record.
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && auth.isLoggedIn()) {
        auth.clear();
        void router.navigate(['/login']);
      }

      return throwError(() => new Error(describe(error)));
    })
  );
};

function describe(error: HttpErrorResponse): string {
  const problem = error.error;

  if (problem?.errors && typeof problem.errors === 'object') {
    const messages = Object.values(problem.errors as Record<string, string[]>).flat();
    if (messages.length) {
      return messages.join(' ');
    }
  }

  if (typeof problem?.detail === 'string') {
    return problem.detail;
  }

  if (error.status === 0) {
    return 'Cannot reach the API. Is the backend running on port 5080?';
  }

  return `Request failed (${error.status}).`;
}
