import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Guards
import { AuthGuard } from './core/guards/auth.guard';

// Components
import { LoginComponent } from './features/auth/components/login/login.component';
import { RegisterComponent } from './features/auth/components/register/register.component';
import { NotFoundComponent } from './shared/components/not-found/not-found.component';

const routes: Routes = [
  // Public routes
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'auth/login', component: LoginComponent },
  { path: 'auth/register', component: RegisterComponent },
  
  // Protected routes
  { 
    path: 'dashboard', 
    loadChildren: () => import('./features/dashboard/dashboard.module').then(m => m.DashboardModule),
    canActivate: [AuthGuard]
  },
  { 
    path: 'recipes', 
    loadChildren: () => import('./features/recipes/recipes.module').then(m => m.RecipesModule),
    canActivate: [AuthGuard]
  },
  { 
    path: 'meal-plans', 
    loadChildren: () => import('./features/meal-plans/meal-plans.module').then(m => m.MealPlansModule),
    canActivate: [AuthGuard]
  },
  { 
    path: 'grocery-lists', 
    loadChildren: () => import('./features/grocery-lists/grocery-lists.module').then(m => m.GroceryListsModule),
    canActivate: [AuthGuard]
  },
  { 
    path: 'profile', 
    loadChildren: () => import('./features/profile/profile.module').then(m => m.ProfileModule),
    canActivate: [AuthGuard]
  },
  { 
    path: 'notifications', 
    loadChildren: () => import('./features/notifications/notifications.module').then(m => m.NotificationsModule),
    canActivate: [AuthGuard]
  },
  
  // Fallback
  { path: '404', component: NotFoundComponent },
  { path: '**', redirectTo: '/404' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    enableTracing: false, // Set to true for debugging
    scrollPositionRestoration: 'top'
  })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
