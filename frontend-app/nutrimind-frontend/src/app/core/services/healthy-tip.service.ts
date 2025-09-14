import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HealthyTip } from '../../shared/models/healthy-tip.models';
import { catchError } from 'rxjs/internal/operators/catchError';
import { map } from 'rxjs/internal/operators/map';
import { throwError } from 'rxjs/internal/observable/throwError';
import { Observable } from 'rxjs/internal/Observable';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class HealthyTipService {
  private readonly apiUrl = 'http://localhost:7066/api';
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  getHealthyContent(): Observable<HealthyTip> {
      const headers = this.getAuthHeaders();
      return this.http.get<any>(`${this.apiUrl}/healthyContent`, { headers })
        .pipe(
          map(response => JSON.parse(response.Data || {})),
          catchError(this.handleError)
        );
    }

    /**
   * Get authentication headers
   */
  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }

  /**
   * Handle HTTP errors
   */
  private handleError = (error: any): Observable<never> => {
    console.error('MealPlanService error:', error);
    let errorMessage = 'An error occurred while processing your request.';
    
    if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    }
    
    return throwError(() => new Error(errorMessage));
  };
    
}