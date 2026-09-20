import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../../core/api.config';
import { MaintenanceRequest } from '../../../../core/models/request.model';

/** The state transitions. Each one is a POST to a named action, not a PATCH of a
 * status field — the server owns which transitions are legal, and an endpoint that
 * accepted an arbitrary new status would hand that decision to the client. */
@Injectable({ providedIn: 'root' })
export class RequestActionsApiService {
  private readonly http = inject(HttpClient);

  approve(id: string): Observable<MaintenanceRequest> {
    return this.http.post<MaintenanceRequest>(`${API_BASE_URL}/requests/${id}/approve`, {});
  }

  reject(id: string, reason: string | null): Observable<MaintenanceRequest> {
    return this.http.post<MaintenanceRequest>(`${API_BASE_URL}/requests/${id}/reject`, { reason });
  }

  complete(id: string, actualCost: number): Observable<MaintenanceRequest> {
    return this.http.post<MaintenanceRequest>(`${API_BASE_URL}/requests/${id}/complete`, { actualCost });
  }
}
