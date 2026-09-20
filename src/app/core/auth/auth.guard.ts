import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Keeps signed-out users off the app's pages.
 *
 * This is navigation convenience, not authorisation. A guard can be defeated by
 * anyone willing to open devtools, and the API would still refuse them — which is
 * where the actual decision is made.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Same caveat: hides the approver-only screens; the server enforces the role. */
export const approverGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isApprover() ? true : router.createUrlTree(['/requests']);
};
