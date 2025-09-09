import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { 
  Recipe, 
  RecipeSearchRequest,
  RecipeSearchFilters,
  ApiResponse,
  PaginatedResponse 
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class RecipeService {
  private readonly API_BASE_URL = 'https://nutrimind-api-eastus2.azurewebsites.net/api';

  constructor(private http: HttpClient) {}

  // Recipe CRUD Operations
  getAllRecipes(pageNumber: number = 1, pageSize: number = 10): Observable<PaginatedResponse<Recipe>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<ApiResponse<PaginatedResponse<Recipe>>>(`${this.API_BASE_URL}/recipes`, { params })
      .pipe(map(response => response.data!));
  }

  getRecipe(id: string): Observable<Recipe> {
    return this.http.get<ApiResponse<Recipe>>(`${this.API_BASE_URL}/recipes/${id}`)
      .pipe(map(response => response.data!));
  }

  createRecipe(recipe: Omit<Recipe, 'id' | 'createdAt' | 'updatedAt'>): Observable<Recipe> {
    return this.http.post<ApiResponse<Recipe>>(`${this.API_BASE_URL}/recipes`, recipe)
      .pipe(map(response => response.data!));
  }

  updateRecipe(id: string, recipe: Partial<Recipe>): Observable<Recipe> {
    return this.http.put<ApiResponse<Recipe>>(`${this.API_BASE_URL}/recipes/${id}`, recipe)
      .pipe(map(response => response.data!));
  }

  deleteRecipe(id: string): Observable<void> {
    return this.http.delete<ApiResponse<void>>(`${this.API_BASE_URL}/recipes/${id}`)
      .pipe(map(response => response.data!));
  }

  // Recipe Search
  searchRecipes(searchRequest: RecipeSearchRequest): Observable<PaginatedResponse<Recipe>> {
    return this.http.post<ApiResponse<PaginatedResponse<Recipe>>>(`${this.API_BASE_URL}/recipes/search`, searchRequest)
      .pipe(map(response => response.data!));
  }

  // AI Recipe Generation
  generateRecipeRecommendations(userId: string, preferences?: any): Observable<Recipe[]> {
    const payload = { userId, preferences };
    return this.http.post<ApiResponse<Recipe[]>>(`${this.API_BASE_URL}/recipes/ai-recommendations`, payload)
      .pipe(map(response => response.data!));
  }

  // Recipe Analytics
  getPopularRecipes(limit: number = 10): Observable<Recipe[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<ApiResponse<Recipe[]>>(`${this.API_BASE_URL}/recipes/popular`, { params })
      .pipe(map(response => response.data!));
  }

  getTrendingRecipes(limit: number = 10): Observable<Recipe[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<ApiResponse<Recipe[]>>(`${this.API_BASE_URL}/recipes/trending`, { params })
      .pipe(map(response => response.data!));
  }
}
