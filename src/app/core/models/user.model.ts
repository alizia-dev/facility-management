export type UserRole = 'Requester' | 'Approver';

export interface LoginRequest {
  /**
   * Required because a user's email is unique per organisation, not globally —
   * two client companies may legitimately both employ ops@contoso.com. The server
   * resolves the organisation from this before it can look the user up at all.
   */
  organizationSlug: string;
  email: string;
  password: string;
}

export interface LoginResult {
  token: string;
  expiresAtUtc: string;
  userId: string;
  fullName: string;
  role: UserRole;
  organizationId: string;
  organizationName: string;
  costThreshold: number;
}
