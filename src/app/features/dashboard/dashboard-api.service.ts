import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api.config';
import { MaintenanceRequest } from '../../core/models/request.model';

/**
 * The dashboard is a view over the same tenant-scoped list endpoint rather than a
 * bespoke summary endpoint. A `/dashboard/summary` endpoint would be a second
 * query path to get tenant scoping right, for a screen that shows four numbers.
 */
@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly http = inject(HttpClient);

  all(): Observable<MaintenanceRequest[]> {
    return this.http.get<MaintenanceRequest[]>(`${API_BASE_URL}/requests`);
  }
}
