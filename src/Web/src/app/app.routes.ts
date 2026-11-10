import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { Login } from './auth/login';
import { AppShell } from './layout/app-shell';

export const routes: Routes = [
  { path: 'login', component: Login },
  {
    path: '',
    component: AppShell,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./home/home').then(module => module.Home)
      },
      {
        path: 'contacts',
        loadComponent: () => import('./contacts/contacts-page').then(module => module.ContactsPage)
      },
      {
        path: 'opportunities',
        loadComponent: () => import('./opportunities/opportunities-page').then(module => module.OpportunitiesPage)
      },
      {
        path: 'pipeline',
        loadComponent: () => import('./pipeline/pipeline-page').then(module => module.PipelinePage)
      },
      {
        path: 'agenda',
        loadComponent: () => import('./follow-ups/follow-ups-page').then(module => module.FollowUpsPage)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
