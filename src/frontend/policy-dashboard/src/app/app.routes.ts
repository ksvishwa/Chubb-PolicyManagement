import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'policies', pathMatch: 'full' },
  {
    path: 'policies',
    loadComponent: () =>
      import('./features/policies/components/policy-list.component').then(m => m.PolicyListComponent)
  },
  {
    path: 'summary',
    loadComponent: () =>
      import('./features/summary/summary.component').then(m => m.SummaryComponent)
  },
  { path: '**', redirectTo: 'policies' }
];
