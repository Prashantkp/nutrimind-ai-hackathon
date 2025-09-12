import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login';
import { RegisterComponent } from './features/auth/register/register';
import { AuthGuard, GuestGuard } from './core/guards/auth.guard';
import { ProfileSetupGuard, ProfileRequiredGuard } from './core/guards/profile.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [GuestGuard]
  },
  {
    path: 'register',
    component: RegisterComponent,
    canActivate: [GuestGuard]
  },
  {
    path: 'profile/setup',
    loadComponent: () => import('./features/profile/profile-setup/profile-setup').then(c => c.ProfileSetupComponent),
    canActivate: [AuthGuard, ProfileSetupGuard]
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard').then(c => c.DashboardComponent),
    canActivate: [AuthGuard, ProfileRequiredGuard]
  },
  {
    path: '**',
    redirectTo: '/dashboard'
  }
];
