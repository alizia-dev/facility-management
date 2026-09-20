import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../../core/api.config';
import { MaintenanceRequest, RequestStatus } from '../../../../core/models/request.model';

@Injectable({ providedIn: 'root' })
export class RequestListApiService {
  private readonly http = inject(HttpClient);

  /**
   * Note the absence of an organisation parameter. There is nothing to pass: the
   * server scopes the list from the bearer token, and the API offers no way to ask
   * for another organisation's requests.
   */
  list(status?: RequestStatus): Observable<MaintenanceRequest[]> {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return this.http.get<MaintenanceRequest[]>(`${API_BASE_URL}/requests`, { params });
  }
}
