import { Injectable, signal, Inject, PLATFORM_ID } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';
import { 
  RegisterRequest, 
  LoginRequest, 
  RefreshTokenRequest, 
  AuthResponse, 
  UserDto, 
  ApiResponse 
} from '../../shared/models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_BASE = 'http://localhost:7066/api/auth';
  private readonly TOKEN_KEY = 'nutrimind_token';
  private readonly REFRESH_TOKEN_KEY = 'nutrimind_refresh_token';

  private currentUserSubject = new BehaviorSubject<UserDto | null>(null);
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);

  // Angular 17 signals for reactive state management
  public currentUser = signal<UserDto | null>(null);
  public isAuthenticated = signal<boolean>(false);

  // Observables for backward compatibility
  public currentUser$ = this.currentUserSubject.asObservable();
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    this.initializeAuthState();
  }

  private initializeAuthState(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return; // Skip initialization on server-side
    }
    
    const token = localStorage.getItem(this.TOKEN_KEY);
    console.log('Initializing auth state - Token exists:', !!token);
    
    if (token && !this.isTokenExpired(token)) {
      console.log('Valid token found, setting authenticated state');
      
      // Set authenticated state immediately based on valid token
      this.isAuthenticated.set(true);
      this.isAuthenticatedSubject.next(true);
      
      // Try to get current user data, but don't clear auth state if it fails
      this.getCurrentUser().subscribe({
        next: (user) => {
          console.log('Successfully retrieved user during initialization:', user);
          this.currentUser.set(user);
          this.currentUserSubject.next(user);
        },
        error: (error) => {
          console.error('Failed to get user during initialization:', error);
          console.warn('API error during initialization, but keeping authenticated state based on valid token');
          
          // Don't create a minimal user object that might override profile status
          // Instead, just set authenticated but leave user as null until we can fetch proper data
          // The guards will handle the case where user is null
          
          // Optionally, you could retry getting user data after a delay
          setTimeout(() => {
            this.retryGetCurrentUser();
          }, 5000);
        }
      });
    } else {
      console.log('No valid token found, clearing auth state');
      this.clearAuthState();
    }
  }

  private retryGetCurrentUser(): void {
    if (this.isUserAuthenticated()) {
      console.log('Retrying getCurrentUser after initialization failure');
      this.getCurrentUser().subscribe({
        next: (user) => {
          console.log('Successfully retrieved user on retry:', user);
          this.currentUser.set(user);
          this.currentUserSubject.next(user);
        },
        error: (error) => {
          console.warn('Retry getCurrentUser also failed:', error);
          // Don't clear auth state - user can stay logged in with minimal data
        }
      });
    }
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.API_BASE}/register`, request)
      .pipe(
        map(response => {
          if (response.success && response.data) {
            this.handleAuthSuccess(response.data);
            return response.data;
          }
          throw new Error(response.message || 'Registration failed');
        }),
        catchError(this.handleError)
      );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.API_BASE}/login`, request)
      .pipe(
        map(response => {
          if (response.success && response.data) {
            this.handleAuthSuccess(response.data);
            return response.data;
          }
          throw new Error(response.message || 'Login failed');
        }),
        catchError(this.handleError)
      );
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    const request: RefreshTokenRequest = { refreshToken };
    return this.http.post<ApiResponse<AuthResponse>>(`${this.API_BASE}/refresh`, request)
      .pipe(
        map(response => {
          if (response.success && response.data) {
            this.handleAuthSuccess(response.data);
            return response.data;
          }
          throw new Error(response.message || 'Token refresh failed');
        }),
        catchError(error => {
          this.logout();
          return this.handleError(error);
        })
      );
  }

  getCurrentUser(): Observable<UserDto> {
    const token = this.getToken();
    console.log('getCurrentUser called - Token exists:', !!token);
    console.log('API URL:', `${this.API_BASE}/me`);
    
    if (!token) {
      console.warn('No token available for getCurrentUser request');
      return throwError(() => new Error('No authentication token available'));
    }

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });

    return this.http.get<ApiResponse<UserDto>>(`${this.API_BASE}/me`, { headers })
      .pipe(
        tap(response => {
          console.log('getCurrentUser API response:', response);
        }),
        map(response => {
          if (response.success && response.data) {
            console.log('getCurrentUser successful, user data:', response.data);
            // Update local user state with latest data from server
            this.setAuthenticatedUser(response.data);
            return response.data;
          }
          console.error('getCurrentUser API response invalid:', response);
          throw new Error(response.message || 'Failed to get current user');
        }),
        catchError(error => {
          console.error('getCurrentUser API error:', error);
          console.error('Error status:', error.status);
          console.error('Error message:', error.message);
          return this.handleError(error);
        })
      );
  }

  /**
   * Update the profile created status for the current user
   * Called after successful profile creation
   */
  updateProfileStatus(hasProfile: boolean = true): void {
    const currentUser = this.currentUser();
    if (currentUser) {
      const updatedUser: UserDto = {
        ...currentUser,
        hasProfile: hasProfile
      };
      this.setAuthenticatedUser(updatedUser);
    }
  }

  /**
   * Check if the current user has a profile
   */
  hasProfile(): boolean {
    const user = this.currentUser();
    return user ? user.hasProfile : false;
  }

  /**
   * Manually refresh user data from server
   * Useful when we need to check latest profile status
   */
  refreshUserData(): Observable<UserDto> {
    return this.getCurrentUser();
  }

  /**
   * Check if user is authenticated by validating token
   */
  isUserAuthenticated(): boolean {
    const token = this.getToken();
    return !!(token && !this.isTokenExpired(token));
  }

  /**
   * Debug method to check auth state
   */
  debugAuthState(): void {
    const token = this.getToken();
    const refreshToken = this.getRefreshToken();
    const user = this.currentUser();
    const isAuth = this.isAuthenticated();

    console.log('=== AUTH DEBUG STATE ===');
    console.log('Token exists:', !!token);
    console.log('Token valid:', this.isUserAuthenticated());
    console.log('Refresh token exists:', !!refreshToken);
    console.log('Is authenticated:', isAuth);
    console.log('Current user:', user);
    console.log('Has profile:', user?.hasProfile);
    console.log('API Base URL:', this.API_BASE);
    console.log('========================');
  }

  logout(): void {
    this.clearAuthState();
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  private handleAuthSuccess(authResponse: AuthResponse): void {
    console.log('Handling auth success:', authResponse);
    
    if (isPlatformBrowser(this.platformId)) {
      try {
        localStorage.setItem(this.TOKEN_KEY, authResponse.token);
        localStorage.setItem(this.REFRESH_TOKEN_KEY, authResponse.refreshToken);
        console.log('Tokens stored successfully');
        console.log('Token:', authResponse.token.substring(0, 50) + '...');
        console.log('Refresh Token:', authResponse.refreshToken.substring(0, 50) + '...');
      } catch (error) {
        console.error('Error storing tokens:', error);
      }
    } else {
      console.log('Not in browser, tokens not stored');
    }
    
    this.setAuthenticatedUser(authResponse.user);
  }

  private setAuthenticatedUser(user: UserDto): void {
    console.log('Setting authenticated user:', user);
    this.currentUser.set(user);
    this.currentUserSubject.next(user);
    this.isAuthenticated.set(true);
    this.isAuthenticatedSubject.next(true);
    console.log('Auth state updated - isAuthenticated:', true, 'hasProfile:', user.hasProfile);
  }

  private clearAuthState(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(this.TOKEN_KEY);
      localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    }
    this.currentUser.set(null);
    this.currentUserSubject.next(null);
    this.isAuthenticated.set(false);
    this.isAuthenticatedSubject.next(false);
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload.exp;
      return Date.now() >= exp * 1000;
    } catch {
      return true;
    }
  }

  private handleError(error: any): Observable<never> {
    let errorMessage = 'An unexpected error occurred';
    
    if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    } else if (typeof error === 'string') {
      errorMessage = error;
    }
    
    console.error('Auth Service Error:', error);
    return throwError(() => new Error(errorMessage));
  }
}
