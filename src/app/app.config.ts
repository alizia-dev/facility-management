import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),

    // Order matters: authInterceptor runs first on the way out (attaching the
    // token) and errorInterceptor is therefore outermost on the way back, so it
    // sees failures from every request including authenticated ones.
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor]))
  ]
};
