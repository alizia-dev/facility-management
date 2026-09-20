import { DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { AuditEntry } from '../../../../core/models/request.model';
import { AuditLogApiService } from './audit-log-api.service';

/**
 * The compliance view of one request: who did what, when, in order.
 * Read-only, because the API offers nothing else.
 */
@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './audit-log.component.html'
})
export class AuditLogComponent {
  readonly requestId = input.required<string>();

  private readonly api = inject(AuditLogApiService);

  protected readonly entries = signal<AuditEntry[]>([]);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      const id = this.requestId();

      this.api.forRequest(id).subscribe({
        next: (entries) => this.entries.set(entries),
        error: (err: Error) => this.error.set(err.message)
      });
    });
  }
}
