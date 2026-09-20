import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api.config';
import { SpendReport } from '../../core/models/organisation.model';

@Injectable({ providedIn: 'root' })
export class SpendReportApiService {
  private readonly http = inject(HttpClient);

  /**
   * Dates are sent as UTC instants. The range is half-open on the server —
   * `fromUtc` inclusive, `toUtc` exclusive — so the caller must pass the start of
   * the day *after* the last day it wants included.
   */
  report(fromUtc: Date, toUtc: Date): Observable<SpendReport> {
    const params = new HttpParams()
      .set('fromUtc', fromUtc.toISOString())
      .set('toUtc', toUtc.toISOString());

    return this.http.get<SpendReport>(`${API_BASE_URL}/reports/spend`, { params });
  }
}
