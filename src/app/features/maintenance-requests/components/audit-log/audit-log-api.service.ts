import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../../core/api.config';
import { AuditEntry } from '../../../../core/models/request.model';

/**
 * Read only — and that is the entire API surface for audit data. There is no
 * update or delete method here because there is no endpoint to call: the trail is
 * append-only in the service layer, in the DbContext, and at the database grant.
 */
@Injectable({ providedIn: 'root' })
export class AuditLogApiService {
  private readonly http = inject(HttpClient);

  forRequest(requestId: string): Observable<AuditEntry[]> {
    return this.http.get<AuditEntry[]>(`${API_BASE_URL}/requests/${requestId}/audit`);
  }
}
