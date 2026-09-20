import { Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { MaintenanceRequest } from '../../../../core/models/request.model';
import { FormErrorsComponent } from '../../../../shared/components/form-errors/form-errors.component';
import { RequestActionsApiService } from './request-actions-api.service';

/**
 * Approve / reject / complete for a single request.
 *
 * Every `canX` getter below is presentation only. The server re-checks the role,
 * the self-approval rule and the legality of the transition, and will answer 403
 * or 409 regardless of what this component chose to render. Deliberately, the
 * "you cannot approve your own request" case is *not* hidden — the button stays
 * visible and the API's refusal is surfaced, because a reviewer should be able to
 * see the server enforcing the rule rather than take a hidden button on trust.
 */
@Component({
  selector: 'app-request-actions',
  standalone: true,
  imports: [FormsModule, FormErrorsComponent],
  templateUrl: './request-actions.component.html'
})
export class RequestActionsComponent {
  readonly request = input.required<MaintenanceRequest>();
  readonly changed = output<MaintenanceRequest>();

  private readonly api = inject(RequestActionsApiService);
  protected readonly auth = inject(AuthService);

  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly showComplete = signal(false);
  protected actualCost: number | null = null;
  protected rejectReason = '';

  protected get canDecide(): boolean {
    return this.auth.isApprover() && this.request().status === 'PendingApproval';
  }

  protected get canComplete(): boolean {
    return this.auth.isApprover() && this.request().status === 'Approved';
  }

  /** Shown as a hint next to the approve button, not used to disable it. */
  protected get isOwnRequest(): boolean {
    return this.request().requestedByUserId === this.auth.session()?.userId;
  }

  protected approve(): void {
    this.run(this.api.approve(this.request().id));
  }

  protected reject(): void {
    this.run(this.api.reject(this.request().id, this.rejectReason || null));
  }

  protected complete(): void {
    if (this.actualCost === null) {
      this.error.set('Enter the actual cost.');
      return;
    }

    this.run(this.api.complete(this.request().id, this.actualCost));
  }

  private run(call: import('rxjs').Observable<MaintenanceRequest>): void {
    this.busy.set(true);
    this.error.set(null);

    call.subscribe({
      next: (updated) => {
        this.busy.set(false);
        this.showComplete.set(false);
        this.actualCost = null;
        this.rejectReason = '';
        this.changed.emit(updated);
      },
      error: (err: Error) => {
        this.busy.set(false);
        this.error.set(err.message);
      }
    });
  }
}
