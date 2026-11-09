import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { Login } from './auth/login';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'dashboard', loadComponent: () => import('./home/home').then(module => module.Home),
    canActivate: [authGuard] },
  { path: 'contacts', loadComponent: () => import('./contacts/contacts-page').then(module => module.ContactsPage),
    canActivate: [authGuard] },
  { path: 'opportunities', loadComponent: () => import('./opportunities/opportunities-page').then(module => module.OpportunitiesPage),
    canActivate: [authGuard] },
  { path: 'pipeline', loadComponent: () => import('./pipeline/pipeline-page').then(module => module.PipelinePage),
    canActivate: [authGuard] },
  { path: 'agenda', loadComponent: () => import('./follow-ups/follow-ups-page').then(module => module.FollowUpsPage),
    canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
