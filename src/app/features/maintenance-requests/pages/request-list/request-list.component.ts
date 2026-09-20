import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { MaintenanceRequest, RequestStatus } from '../../../../core/models/request.model';
import { AuditLogComponent } from '../../components/audit-log/audit-log.component';
import { RequestActionsComponent } from '../../components/request-actions/request-actions.component';
import { RequestListApiService } from './request-list-api.service';

@Component({
  selector: 'app-request-list',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    RouterLink,
    RequestActionsComponent,
    AuditLogComponent
  ],
  templateUrl: './request-list.component.html'
})
export class RequestListComponent {
  private readonly api = inject(RequestListApiService);
  protected readonly auth = inject(AuthService);

  protected readonly requests = signal<MaintenanceRequest[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(true);
  protected readonly expandedId = signal<string | null>(null);

  protected statusFilter: RequestStatus | '' = '';

  protected readonly statuses: RequestStatus[] = [
    'Raised',
    'PendingApproval',
    'Approved',
    'Rejected',
    'Completed'
  ];

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.list(this.statusFilter || undefined).subscribe({
      next: (requests) => {
        this.requests.set(requests);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      }
    });
  }

  /** Replaces the row in place so the table does not flicker back to the server. */
  protected onChanged(updated: MaintenanceRequest): void {
    this.requests.update((rows) => rows.map((r) => (r.id === updated.id ? updated : r)));
  }

  protected toggle(id: string): void {
    this.expandedId.update((current) => (current === id ? null : id));
  }
}
