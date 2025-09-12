import { Injectable, inject } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, take, catchError, switchMap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { ProfileService } from '../services/profile.service';

@Injectable({
  providedIn: 'root'
})
export class ProfileGuard implements CanActivate {
  private authService = inject(AuthService);
  private profileService = inject(ProfileService);
  private router = inject(Router);

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    return this.authService.currentUser$.pipe(
      take(1),
      switchMap(user => {
        if (!user) {
          // Not authenticated, redirect to login
          this.router.navigate(['/login']);
          return of(false);
        }

        // Check if trying to access profile setup
        if (state.url.includes('/profile/setup')) {
          if (user.hasProfile) {
            // Profile already exists, redirect to dashboard
            this.router.navigate(['/dashboard']);
            return of(false);
          }
          // Profile doesn't exist, allow access to setup
          return of(true);
        }

        // Check if trying to access dashboard or other protected routes
        if (state.url.includes('/dashboard')) {
          if (!user.hasProfile) {
            // No profile exists, redirect to profile setup
            this.router.navigate(['/profile/setup']);
            return of(false);
          }
          // Profile exists, allow access
          return of(true);
        }

        // For other routes, allow access if authenticated
        return of(true);
      }),
      catchError(error => {
        console.error('ProfileGuard error:', error);
        this.router.navigate(['/login']);
        return of(false);
      })
    );
  }
}

/**
 * Guard specifically for profile setup page
 * Prevents access if profile already exists
 */
@Injectable({
  providedIn: 'root'
})
export class ProfileSetupGuard implements CanActivate {
  private authService = inject(AuthService);
  private router = inject(Router);

  canActivate(): Observable<boolean> {
    return this.authService.currentUser$.pipe(
      take(1),
      map(user => {
        console.log('ProfileSetupGuard: Current user:', user);
        console.log('ProfileSetupGuard: User hasProfile:', user?.hasProfile);
        
        if (!user) {
          console.log('ProfileSetupGuard: No user, redirecting to login');
          this.router.navigate(['/login']);
          return false;
        }

        if (user.hasProfile) {
          console.log('ProfileSetupGuard: Profile already exists, redirecting to dashboard');
          this.router.navigate(['/dashboard']);
          return false;
        }

        console.log('ProfileSetupGuard: No profile exists, allowing access to setup');
        return true;
      }),
      catchError(error => {
        console.error('ProfileSetupGuard error:', error);
        this.router.navigate(['/login']);
        return of(false);
      })
    );
  }
}

/**
 * Guard for dashboard and other protected routes that require a profile
 */
@Injectable({
  providedIn: 'root'
})
export class ProfileRequiredGuard implements CanActivate {
  private authService = inject(AuthService);
  private router = inject(Router);

  canActivate(): Observable<boolean> {
    return this.authService.currentUser$.pipe(
      take(1),
      switchMap(user => {
        console.log('ProfileRequiredGuard: Current user:', user);
        console.log('ProfileRequiredGuard: User hasProfile:', user?.hasProfile);
        
        if (!user) {
          console.log('ProfileRequiredGuard: No user data, checking if authenticated...');
          
          // If no user data but we're authenticated, try to get user data
          if (this.authService.isUserAuthenticated()) {
            console.log('ProfileRequiredGuard: User is authenticated but no data, fetching user...');
            return this.authService.getCurrentUser().pipe(
              map(fetchedUser => {
                console.log('ProfileRequiredGuard: Fetched user:', fetchedUser);
                if (!fetchedUser.hasProfile) {
                  console.log('ProfileRequiredGuard: Fetched user has no profile, redirecting to setup');
                  this.router.navigate(['/profile/setup']);
                  return false;
                }
                console.log('ProfileRequiredGuard: Fetched user has profile, allowing dashboard access');
                return true;
              }),
              catchError(() => {
                console.log('ProfileRequiredGuard: Failed to fetch user, redirecting to login');
                this.router.navigate(['/login']);
                return of(false);
              })
            );
          } else {
            console.log('ProfileRequiredGuard: Not authenticated, redirecting to login');
            this.router.navigate(['/login']);
            return of(false);
          }
        }

        if (!user.hasProfile) {
          console.log('ProfileRequiredGuard: No profile found, redirecting to profile setup');
          this.router.navigate(['/profile/setup']);
          return of(false);
        }

        console.log('ProfileRequiredGuard: Profile exists, allowing access to dashboard');
        return of(true);
      }),
      catchError(error => {
        console.error('ProfileRequiredGuard error:', error);
        this.router.navigate(['/login']);
        return of(false);
      })
    );
  }
}
