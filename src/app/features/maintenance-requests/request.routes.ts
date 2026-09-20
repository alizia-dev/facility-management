import { Routes } from '@angular/router';

/**
 * Lazy-loaded feature routes. The login screen and the shell do not pay for this
 * feature's code until the user navigates here.
 */
export const REQUEST_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/request-list/request-list.component').then((m) => m.RequestListComponent)
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./pages/request-create/request-create.component').then((m) => m.RequestCreateComponent)
  }
];
