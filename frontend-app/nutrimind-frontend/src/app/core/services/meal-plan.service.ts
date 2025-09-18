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
    console.log('🔍 Loading meal plans for week:', weekIdentifier);
    const headers = this.getAuthHeaders();
    return this.http.get<any>(`${this.apiUrl}/mealplans?week=${weekIdentifier}`, { headers })
      .pipe(
        map(response => {
          console.log('📈 Raw getMealPlansForWeek response:', response);
          
          // Handle the API response structure: {Success: true, Data: [...], Message: "..."}
          let mappedResponse: MealPlan[] = [];
          
          if (response && response.Success && response.Data) {
            mappedResponse = Array.isArray(response.Data) ? response.Data : [response.Data];
          } else if (Array.isArray(response)) {
            // Direct array response
            mappedResponse = response;
          } else if (response && typeof response === 'object' && !Array.isArray(response)) {
            // Single meal plan object
            mappedResponse = [response];
          }
          
          console.log('📊 Mapped meal plans:', mappedResponse);
          return mappedResponse;
        }),
        catchError(error => {
          console.error('❌ Error in getMealPlansForWeek API call:', error);
          return this.handleError(error);
        })
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
    console.log('🚀 generateMealPlan request:', request);
    const headers = this.getAuthHeaders();
    return this.http.post<any>(`${this.apiUrl}/mealplans`, request, { headers })
      .pipe(
        map(response => {
          console.log('🔍 Raw API response from generateMealPlan:', response);
          
          // Handle different response structures
          let mappedResponse: MealPlanGenerationResponse;
          
          if (response && response.Success && response.Data) {
            // API wrapper format: {Success: true, Data: {orchestrationId: "..."}, Message: "..."}
            mappedResponse = response.Data;
          } else if (response && response.data) {
            mappedResponse = response.data;
          } else if (response && response.orchestrationId) {
            // Response is already in the correct format
            mappedResponse = response;
          } else if (response) {
            // Try to extract orchestrationId from any level
            mappedResponse = {
              OrchestrationId: response.orchestrationId || response.Data?.orchestrationId || null,
              Status: 'Started' as const
            };
          } else {
            console.error('❌ Unexpected response structure:', response);
            throw new Error('Invalid response structure from API');
          }
          
          console.log('📊 Mapped response:', mappedResponse);
          
          // Validate required fields
          if (!mappedResponse.OrchestrationId) {
            console.error('❌ Missing OrchestrationId in response:', mappedResponse);
            throw new Error('Missing OrchestrationId in API response');
          }
          
          return mappedResponse;
        }),
        catchError(error => {
          console.error('❌ Error in generateMealPlan API call:', error);
          return this.handleError(error);
        })
      );
  }

  /**
   * Check the status of meal plan generation
   */
  getMealPlanStatus(OrchestrationId: string): Observable<any> {
    console.log('🔍 Checking meal plan status for OrchestrationId:', OrchestrationId);
    const headers = this.getAuthHeaders();
    return this.http.get<any>(`${this.apiUrl}/mealplans/status/${OrchestrationId}`, { headers })
      .pipe(
        map(response => {
          console.log('📈 Raw status response:', response);
          const mappedResponse = response.data || response;
          console.log('📊 Mapped status response:', mappedResponse);
          return mappedResponse;
        }),
        catchError(error => {
          console.error('❌ Error in getMealPlanStatus API call:', error);
          return this.handleError(error);
        })
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
    console.error('❌ MealPlanService error details:', {
      status: error.status,
      statusText: error.statusText,
      message: error.message,
      error: error.error,
      url: error.url
    });
    
    let errorMessage = 'An error occurred while processing your request.';
    
    if (error.status === 0) {
      errorMessage = 'Network error: Unable to connect to the API server. Please check if the server is running.';
    } else if (error.status === 401) {
      errorMessage = 'Authentication error: Please log in again.';
    } else if (error.status === 403) {
      errorMessage = 'Access denied: You do not have permission to perform this action.';
    } else if (error.status === 404) {
      errorMessage = 'Resource not found: The requested endpoint does not exist.';
    } else if (error.status >= 500) {
      errorMessage = 'Server error: Please try again later.';
    } else if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    }
    
    return throwError(() => new Error(errorMessage));
  };
}
