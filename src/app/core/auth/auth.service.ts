import { Injectable, computed, signal } from '@angular/core';
import { LoginResult, UserRole } from '../models/user.model';

const STORAGE_KEY = 'facilities-mgmt.session';

/**
 * Holds the current session.
 *
 * Storage: the token goes in localStorage so a page refresh does not log you out.
 * The honest trade-off is that localStorage is readable by any script on the
 * origin, so a successful XSS gets the token. The production answer is an
 * httpOnly, SameSite cookie with a CSRF token, which the browser will not hand to
 * JavaScript at all. That is a different server-side auth flow than the brief
 * needs, so it is recorded in DECISIONS.md rather than built.
 *
 * Nothing here is a security control. The role below decides which buttons to
 * draw; the server decides what actually happens. Every rule this file appears to
 * enforce is enforced again in the API, and the integration tests bypass this code
 * entirely to prove it.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly sessionSignal = signal<LoginResult | null>(readStoredSession());

  readonly session = this.sessionSignal.asReadonly();
  readonly isLoggedIn = computed(() => this.sessionSignal() !== null);
  readonly fullName = computed(() => this.sessionSignal()?.fullName ?? '');
  readonly organizationName = computed(() => this.sessionSignal()?.organizationName ?? '');
  readonly costThreshold = computed(() => this.sessionSignal()?.costThreshold ?? 0);
  readonly role = computed<UserRole | null>(() => this.sessionSignal()?.role ?? null);
  readonly isApprover = computed(() => this.role() === 'Approver');

  get token(): string | null {
    return this.sessionSignal()?.token ?? null;
  }

  setSession(result: LoginResult): void {
    this.sessionSignal.set(result);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(result));
  }

  clear(): void {
    this.sessionSignal.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }
}

function readStoredSession(): LoginResult | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    const session = JSON.parse(raw) as LoginResult;

    // Drop an expired token client-side so the user sees the login page rather
    // than a wall of 401s. The server does not trust this check — it validates
    // the token's own lifetime with zero clock skew.
    return new Date(session.expiresAtUtc) > new Date() ? session : null;
  } catch {
    return null;
  }
}
