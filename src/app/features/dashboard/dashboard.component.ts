import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { MaintenanceRequest } from '../../core/models/request.model';
import { DashboardApiService } from './dashboard-api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
  private readonly api = inject(DashboardApiService);
  protected readonly auth = inject(AuthService);

  protected readonly requests = signal<MaintenanceRequest[]>([]);
  protected readonly error = signal<string | null>(null);

  protected readonly pending = computed(() =>
    this.requests().filter((r) => r.status === 'PendingApproval')
  );

  protected readonly approved = computed(() =>
    this.requests().filter((r) => r.status === 'Approved')
  );

  protected readonly completed = computed(() =>
    this.requests().filter((r) => r.status === 'Completed')
  );

  protected readonly totalSpend = computed(() =>
    this.completed().reduce((sum, r) => sum + (r.actualCost ?? 0), 0)
  );

  constructor() {
    this.api.all().subscribe({
      next: (requests) => this.requests.set(requests),
      error: (err: Error) => this.error.set(err.message)
    });
  }
}
