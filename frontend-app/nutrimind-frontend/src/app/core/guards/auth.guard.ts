import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, take, switchMap, delay, timeout, catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(): Observable<boolean> {
    // First check if we have a valid token immediately
    const hasValidToken = this.authService.isUserAuthenticated();
    console.log('AuthGuard: hasValidToken =', hasValidToken);
    
    if (hasValidToken) {
      // If we have a valid token, allow access immediately
      console.log('AuthGuard: Valid token found, allowing access');
      return of(true);
    }
    
    // If no valid token, check the authentication state observable
    return this.authService.isAuthenticated$.pipe(
      take(1),
      switchMap(isAuthenticated => {
        console.log('AuthGuard: isAuthenticated from observable =', isAuthenticated);
        
        if (!isAuthenticated) {
          console.log('AuthGuard: Not authenticated, redirecting to login');
          this.router.navigate(['/login']);
          return of(false);
        }
        return of(true);
      }),
      catchError(error => {
        console.error('AuthGuard error:', error);
        this.router.navigate(['/login']);
        return of(false);
      })
    );
  }
}

@Injectable({
  providedIn: 'root'
})
export class GuestGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(): Observable<boolean> {
    // Check for valid token immediately
    const hasValidToken = this.authService.isUserAuthenticated();
    console.log('GuestGuard: hasValidToken =', hasValidToken);
    
    if (hasValidToken) {
      console.log('GuestGuard: User is authenticated, redirecting to dashboard');
      this.router.navigate(['/dashboard']);
      return of(false);
    }

    return this.authService.isAuthenticated$.pipe(
      take(1),
      map(isAuthenticated => {
        console.log('GuestGuard: isAuthenticated from observable =', isAuthenticated);
        
        if (isAuthenticated) {
          this.router.navigate(['/dashboard']);
          return false;
        }
        return true;
      }),
      catchError(error => {
        console.error('GuestGuard error:', error);
        return of(true); // Allow access to login/register on error
      })
    );
  }
}
