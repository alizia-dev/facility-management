import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { FormErrorsComponent } from '../../shared/components/form-errors/form-errors.component';
import { LoginApiService } from './login-api.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, FormErrorsComponent],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private readonly api = inject(LoginApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);

  // The organisation is part of the credentials because email is unique per
  // organisation, not globally.
  protected readonly form = inject(FormBuilder).nonNullable.group({
    organizationSlug: ['northwind', Validators.required],
    email: ['requester@northwind.test', [Validators.required, Validators.email]],
    password: ['Password123!', Validators.required]
  });

  protected submit(): void {
    if (this.form.invalid || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.api.login(this.form.getRawValue()).subscribe({
      next: (result) => {
        this.auth.setSession(result);
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
        void this.router.navigateByUrl(returnUrl);
      },
      error: (err: Error) => {
        this.busy.set(false);
        // The server answers a bad slug, a bad email and a bad password
        // identically, so this message stays vague by design.
        this.error.set(err.message);
      }
    });
  }
}
