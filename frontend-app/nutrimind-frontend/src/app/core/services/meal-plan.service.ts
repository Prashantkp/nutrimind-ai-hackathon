import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { 
  MealPlan, 
  GenerateMealPlanRequest, 
  MealPlanGenerationResponse,
  WeekViewData
} from '../../shared/models/meal-plan.models';

@Injectable({
  providedIn: 'root'
})
export class MealPlanService {
  private readonly apiUrl = 'http://localhost:7066/api';
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  /**
   * Get meal plans for a specific week
   */
  getMealPlansForWeek(weekIdentifier: string): Observable<MealPlan[]> {
    const headers = this.getAuthHeaders();
    return this.http.get<any>(`${this.apiUrl}/mealplans?week=${weekIdentifier}`, { headers })
      .pipe(
        map(response => response.data || []),
        catchError(this.handleError)
      );
  }

  /**
   * Get a specific meal plan by ID
   */
  getMealPlan(id: string): Observable<MealPlan> {
    const headers = this.getAuthHeaders();
    return this.http.get<any>(`${this.apiUrl}/mealplans/${id}`, { headers })
      .pipe(
        map(response => response.data),
        catchError(this.handleError)
      );
  }

  /**
   * Generate a new meal plan for a week
   */
  generateMealPlan(request: GenerateMealPlanRequest): Observable<MealPlanGenerationResponse> {
    const headers = this.getAuthHeaders();
    return this.http.post<any>(`${this.apiUrl}/mealplans`, request, { headers })
      .pipe(
        map(response => response.data),
        catchError(this.handleError)
      );
  }

  /**
   * Check the status of meal plan generation
   */
  getMealPlanStatus(orchestrationId: string): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.http.get<any>(`${this.apiUrl}/mealplans/status/${orchestrationId}`, { headers })
      .pipe(
        map(response => response.data),
        catchError(this.handleError)
      );
  }

  /**
   * Update an existing meal plan
   */
  updateMealPlan(id: string, updates: Partial<MealPlan>): Observable<MealPlan> {
    const headers = this.getAuthHeaders();
    return this.http.put<any>(`${this.apiUrl}/mealplans/${id}`, updates, { headers })
      .pipe(
        map(response => response.data),
        catchError(this.handleError)
      );
  }

  /**
   * Get current week identifier in YYYY-Www format
   */
  getCurrentWeekIdentifier(): string {
    const now = new Date();
    const startOfYear = new Date(now.getFullYear(), 0, 1);
    const days = Math.floor((now.getTime() - startOfYear.getTime()) / (24 * 60 * 60 * 1000));
    const weekNumber = Math.ceil(days / 7);
    return `${now.getFullYear()}-W${weekNumber.toString().padStart(2, '0')}`;
  }

  /**
   * Get week identifiers for the next few weeks
   */
  getUpcomingWeeks(count: number = 4): WeekViewData[] {
    const weeks: WeekViewData[] = [];
    const today = new Date();
    
    for (let i = 0; i < count; i++) {
      const weekStart = new Date(today);
      weekStart.setDate(today.getDate() + (i * 7) - today.getDay() + 1); // Monday of the week
      
      const weekEnd = new Date(weekStart);
      weekEnd.setDate(weekStart.getDate() + 6); // Sunday of the week
      
      const year = weekStart.getFullYear();
      const startOfYear = new Date(year, 0, 1);
      const days = Math.floor((weekStart.getTime() - startOfYear.getTime()) / (24 * 60 * 60 * 1000));
      const weekNumber = Math.ceil(days / 7);
      const weekIdentifier = `${year}-W${weekNumber.toString().padStart(2, '0')}`;
      
      weeks.push({
        weekIdentifier,
        weekStartDate: weekStart,
        weekEndDate: weekEnd,
        displayText: this.getWeekDisplayText(weekStart, weekEnd, i === 0)
      });
    }
    
    return weeks;
  }

  /**
   * Parse week identifier to get start and end dates
   */
  parseWeekIdentifier(weekIdentifier: string): { startDate: Date, endDate: Date } | null {
    const match = weekIdentifier.match(/^(\d{4})-W(\d{2})$/);
    if (!match) return null;
    
    const year = parseInt(match[1]);
    const weekNum = parseInt(match[2]);
    
    const startOfYear = new Date(year, 0, 1);
    const startDate = new Date(startOfYear);
    startDate.setDate(startOfYear.getDate() + ((weekNum - 1) * 7) - startOfYear.getDay() + 1);
    
    const endDate = new Date(startDate);
    endDate.setDate(startDate.getDate() + 6);
    
    return { startDate, endDate };
  }

  /**
   * Get display text for a week
   */
  private getWeekDisplayText(startDate: Date, endDate: Date, isCurrentWeek: boolean): string {
    const options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' };
    const startStr = startDate.toLocaleDateString('en-US', options);
    const endStr = endDate.toLocaleDateString('en-US', options);
    
    const prefix = isCurrentWeek ? 'This Week: ' : '';
    return `${prefix}${startStr} - ${endStr}`;
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
