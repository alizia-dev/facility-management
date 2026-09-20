import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { Site } from '../../../../core/models/organisation.model';
import { FormErrorsComponent } from '../../../../shared/components/form-errors/form-errors.component';
import { RequestCreateApiService } from './request-create-api.service';

@Component({
  selector: 'app-request-create',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe, FormErrorsComponent],
  templateUrl: './request-create.component.html'
})
export class RequestCreateComponent {
  private readonly api = inject(RequestCreateApiService);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  protected readonly sites = signal<Site[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    siteId: ['', Validators.required],
    description: ['', [Validators.required, Validators.maxLength(4000)]],
    estimatedCost: [0, [Validators.required, Validators.min(0)]]
  });

  constructor() {
    this.api.sites().subscribe({
      next: (sites) => {
        this.sites.set(sites);
        if (sites.length) {
          this.form.controls.siteId.setValue(sites[0].id);
        }
      },
      error: (err: Error) => this.error.set(err.message)
    });
  }

  /**
   * Mirrors the server's routing rule so the form can warn what will happen.
   * The server decides for real, against the threshold it reads from the database
   * and snapshots onto the request — this is a preview, not the rule.
   */
  protected get willNeedApproval(): boolean {
    return this.form.controls.estimatedCost.value >= this.auth.costThreshold();
  }

  protected submit(): void {
    if (this.form.invalid || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.api.create(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigate(['/requests']),
      error: (err: Error) => {
        this.busy.set(false);
        this.error.set(err.message);
      }
    });
  }
}
