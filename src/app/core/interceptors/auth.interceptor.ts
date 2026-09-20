import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { API_BASE_URL } from '../api.config';

/**
 * Attaches the bearer token to API calls.
 *
 * There is deliberately no tenant or organisation header here. The reference
 * layout this project was given suggested a `tenant.interceptor.ts` that injects
 * an org header — it is not built, because the server resolves the tenant from
 * validated JWT claims and ignores any client-supplied organisation id. Sending a
 * header the server must ignore is worse than sending nothing: it looks
 * authoritative, and the next person to touch the backend may decide to trust it.
 * The token is the tenant assertion, and it is signed.
 *
 * The URL check matters. Without it, a future call to any third-party endpoint
 * would quietly post this user's credentials to someone else's server.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthService).token;

  if (!token || !request.url.startsWith(API_BASE_URL)) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
