import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },

  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
  },
  {
    path: 'requests',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/maintenance-requests/request.routes').then((m) => m.REQUEST_ROUTES)
  },
  {
    path: 'reports',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/spend-reports/spend-report.component').then((m) => m.SpendReportComponent)
  },

  { path: '**', redirectTo: 'dashboard' }
];
