import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { UserProfile, CreateUserProfileRequest } from '../../shared/models/profile.models';
import { ApiResponse } from '../../shared/models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  private readonly API_BASE = 'http://localhost:7066/api/profile';

  constructor(private http: HttpClient) {}

  createProfile(request: CreateUserProfileRequest): Observable<UserProfile> {
    return this.http.post<ApiResponse<UserProfile>>(`${this.API_BASE}`, request)
      .pipe(
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || 'Profile creation failed');
        }),
        catchError(this.handleError)
      );
  }

  getProfile(): Observable<UserProfile> {
    return this.http.get<ApiResponse<UserProfile>>(`${this.API_BASE}`)
      .pipe(
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || 'Failed to get profile');
        }),
        catchError(this.handleError)
      );
  }

  updateProfile(request: Partial<CreateUserProfileRequest>): Observable<UserProfile> {
    return this.http.put<ApiResponse<UserProfile>>(`${this.API_BASE}`, request)
      .pipe(
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || 'Profile update failed');
        }),
        catchError(this.handleError)
      );
  }

  deleteProfile(): Observable<void> {
    return this.http.delete<ApiResponse<void>>(`${this.API_BASE}`)
      .pipe(
        map(response => {
          if (!response.success) {
            throw new Error(response.message || 'Profile deletion failed');
          }
        }),
        catchError(this.handleError)
      );
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
    
    console.error('Profile Service Error:', error);
    throw new Error(errorMessage);
  }
}
