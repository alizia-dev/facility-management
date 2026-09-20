import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/api.config';
import { LoginRequest, LoginResult } from '../../core/models/user.model';

/**
 * One API service per feature, scoped to that feature's calls. The alternative —
 * a single app-wide ApiService — turns into a grab bag that every component
 * depends on and nobody can safely change.
 */
@Injectable({ providedIn: 'root' })
export class LoginApiService {
  private readonly http = inject(HttpClient);

  login(request: LoginRequest): Observable<LoginResult> {
    return this.http.post<LoginResult>(`${API_BASE_URL}/auth/login`, request);
  }
}
