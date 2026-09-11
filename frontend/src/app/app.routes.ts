import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./features/player-search/player-search').then((m) => m.PlayerSearch),
  },
  {
    path: 'player/:id',
    loadComponent: () =>
      import('./features/player-dashboard/player-dashboard').then((m) => m.PlayerDashboard),
  },
  { path: '**', redirectTo: '' },
];
