import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { SpendReport } from '../../core/models/organisation.model';
import { SpendReportApiService } from './spend-report-api.service';

@Component({
  selector: 'app-spend-report',
  standalone: true,
  imports: [CurrencyPipe, FormsModule],
  templateUrl: './spend-report.component.html'
})
export class SpendReportComponent {
  private readonly api = inject(SpendReportApiService);
  protected readonly auth = inject(AuthService);

  protected readonly report = signal<SpendReport | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(false);

  // Defaults to the last full calendar month plus the current one, which is the
  // shape of the question the brief quotes: "what did we spend at Site 12 last
  // month?"
  protected from = isoDate(startOfMonth(-1));
  protected to = isoDate(startOfMonth(1));

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    // The date inputs give local calendar dates; they are interpreted as UTC
    // midnight so the range means the same thing regardless of where the browser
    // is. A facilities company with sites in several time zones cannot have a
    // report whose boundaries move with the viewer.
    const fromUtc = new Date(`${this.from}T00:00:00Z`);
    const toUtc = new Date(`${this.to}T00:00:00Z`);

    this.api.report(fromUtc, toUtc).subscribe({
      next: (report) => {
        this.report.set(report);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      }
    });
  }
}

function startOfMonth(offsetMonths: number): Date {
  const now = new Date();
  return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() + offsetMonths, 1));
}

function isoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}
