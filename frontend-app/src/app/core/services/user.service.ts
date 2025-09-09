import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { 
  User, 
  UserPreferences, 
  ApiResponse 
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly API_BASE_URL = 'https://nutrimind-api-eastus2.azurewebsites.net/api';

  constructor(private http: HttpClient) {}

  // User Profile Operations
  getUserProfile(userId: string): Observable<User> {
    return this.http.get<ApiResponse<User>>(`${this.API_BASE_URL}/users/${userId}`)
      .pipe(map(response => response.data!));
  }

  updateUserProfile(userId: string, userData: Partial<User>): Observable<User> {
    return this.http.put<ApiResponse<User>>(`${this.API_BASE_URL}/users/${userId}`, userData)
      .pipe(map(response => response.data!));
  }

  deleteUserProfile(userId: string): Observable<void> {
    return this.http.delete<ApiResponse<void>>(`${this.API_BASE_URL}/users/${userId}`)
      .pipe(map(response => response.data!));
  }

  // User Preferences Operations
  getUserPreferences(userId: string): Observable<UserPreferences> {
    return this.http.get<ApiResponse<UserPreferences>>(`${this.API_BASE_URL}/users/${userId}/preferences`)
      .pipe(map(response => response.data!));
  }

  createUserPreferences(userId: string, preferences: Omit<UserPreferences, 'id' | 'userId'>): Observable<UserPreferences> {
    return this.http.post<ApiResponse<UserPreferences>>(`${this.API_BASE_URL}/users/${userId}/preferences`, preferences)
      .pipe(map(response => response.data!));
  }

  updateUserPreferences(userId: string, preferencesId: string, preferences: Partial<UserPreferences>): Observable<UserPreferences> {
    return this.http.put<ApiResponse<UserPreferences>>(`${this.API_BASE_URL}/users/${userId}/preferences/${preferencesId}`, preferences)
      .pipe(map(response => response.data!));
  }

  deleteUserPreferences(userId: string, preferencesId: string): Observable<void> {
    return this.http.delete<ApiResponse<void>>(`${this.API_BASE_URL}/users/${userId}/preferences/${preferencesId}`)
      .pipe(map(response => response.data!));
  }
}
