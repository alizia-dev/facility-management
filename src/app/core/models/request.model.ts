export type RequestStatus =
  | 'Raised'
  | 'PendingApproval'
  | 'Approved'
  | 'Rejected'
  | 'Completed';

export interface MaintenanceRequest {
  id: string;
  siteId: string;
  siteName: string;
  description: string;
  estimatedCost: number;
  actualCost: number | null;
  status: RequestStatus;
  /** The organisation's threshold as it stood when this request was raised. */
  thresholdAtSubmission: number;
  requestedByUserId: string;
  requestedByName: string;
  approvedByName: string | null;
  approvedAt: string | null;
  completedAt: string | null;
  requiresApproval: boolean;
  createdAt: string;
}

export interface CreateMaintenanceRequest {
  siteId: string;
  description: string;
  estimatedCost: number;
}

export interface AuditEntry {
  id: number;
  entityType: string;
  entityId: string;
  action: string;
  performedByName: string;
  /** JSON, rendered verbatim — the trail is evidence, not a formatted message. */
  details: string | null;
  occurredAt: string;
}
