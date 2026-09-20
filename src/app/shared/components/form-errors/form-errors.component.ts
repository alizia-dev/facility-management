import { Component, input } from '@angular/core';

/**
 * One place to render a server or validation message, so every screen reports
 * failure the same way. Server messages arrive already flattened by the error
 * interceptor.
 */
@Component({
  selector: 'app-form-errors',
  standalone: true,
  template: `
    @if (message()) {
      <p class="error" role="alert">{{ message() }}</p>
    }
  `
})
export class FormErrorsComponent {
  readonly message = input<string | null>(null);
}
