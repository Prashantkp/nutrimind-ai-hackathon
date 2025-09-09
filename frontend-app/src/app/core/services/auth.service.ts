import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { 
  LoginRequest, 
  RegisterRequest, 
  AuthResponse, 
  User, 
  ApiResponse 
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_BASE_URL = environment.apiUrl;
  private readonly TOKEN_KEY = 'nutrimind_token';
  private readonly REFRESH_TOKEN_KEY = 'nutrimind_refresh_token';
  private readonly USER_KEY = 'nutrimind_user';

  private currentUserSubject = new BehaviorSubject<User | null>(null);
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);

  public currentUser$ = this.currentUserSubject.asObservable();
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Check for existing auth state on service init
    this.initializeAuthState();
  }

  private initializeAuthState(): void {
    const token = this.getToken();
    const user = this.getStoredUser();
    
    if (token && user && !this.isTokenExpired(token)) {
      this.currentUserSubject.next(user);
      this.isAuthenticatedSubject.next(true);
    } else {
      this.clearAuthData();
    }
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    console.log('AuthService: Attempting login with:', credentials);
    console.log('AuthService: API URL:', `${this.API_BASE_URL}/auth/login`);
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    });
    
    return this.http.post<ApiResponse<AuthResponse>>(
      `${this.API_BASE_URL}/auth/login`, 
      credentials,
      { headers: headers }
    ).pipe(
      map(response => {
        console.log('AuthService: Raw response from API:', response);
        // Check if response has data property
        if (!response.data) {
          throw new Error('No data in response: ' + JSON.stringify(response));
        }
        return response.data!;
      }),
      tap(authResponse => {
        console.log('AuthService: Processed auth response:', authResponse);
        this.setAuthData(authResponse);
        this.currentUserSubject.next(authResponse.user);
        this.isAuthenticatedSubject.next(true);
      })
    );
  }

  register(userData: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<ApiResponse<AuthResponse>>(
      `${this.API_BASE_URL}/auth/register`, 
      userData
    ).pipe(
      map(response => response.data!),
      tap(authResponse => {
        this.setAuthData(authResponse);
        this.currentUserSubject.next(authResponse.user);
        this.isAuthenticatedSubject.next(true);
      })
    );
  }

  logout(): void {
    this.clearAuthData();
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/auth/login']);
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    return this.http.post<ApiResponse<AuthResponse>>(
      `${this.API_BASE_URL}/auth/refresh`, 
      { refreshToken }
    ).pipe(
      map(response => response.data!),
      tap(authResponse => {
        this.setAuthData(authResponse);
        this.currentUserSubject.next(authResponse.user);
        this.isAuthenticatedSubject.next(true);
      })
    );
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    return token !== null && !this.isTokenExpired(token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  private getStoredUser(): User | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    if (userJson) {
      try {
        return JSON.parse(userJson);
      } catch (error) {
        console.error('Error parsing stored user data:', error);
        return null;
      }
    }
    return null;
  }

  private setAuthData(authResponse: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, authResponse.token);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, authResponse.refreshToken);
    localStorage.setItem(this.USER_KEY, JSON.stringify(authResponse.user));
  }

  private clearAuthData(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      return payload.exp < currentTime;
    } catch (error) {
      console.error('Error checking token expiration:', error);
      return true;
    }
  }

  // Utility method to get authorization header
  getAuthHeaders(): { [key: string]: string } {
    const token = this.getToken();
    return token ? { 'Authorization': `Bearer ${token}` } : {};
  }
}
