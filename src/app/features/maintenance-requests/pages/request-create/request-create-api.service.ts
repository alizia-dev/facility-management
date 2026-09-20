import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../../core/api.config';
import { Site } from '../../../../core/models/organisation.model';
import { CreateMaintenanceRequest, MaintenanceRequest } from '../../../../core/models/request.model';

@Injectable({ providedIn: 'root' })
export class RequestCreateApiService {
  private readonly http = inject(HttpClient);

  /** Only this organisation's sites come back, so the picker cannot offer an id
   * from another tenant — and if one were supplied by hand, the API returns 404. */
  sites(): Observable<Site[]> {
    return this.http.get<Site[]>(`${API_BASE_URL}/sites`);
  }

  create(request: CreateMaintenanceRequest): Observable<MaintenanceRequest> {
    return this.http.post<MaintenanceRequest>(`${API_BASE_URL}/requests`, request);
  }
}
