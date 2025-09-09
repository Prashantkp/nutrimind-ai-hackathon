import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { Observable, of } from 'rxjs';
import { RecipeService } from '../../../../core/services/recipe.service';
import { Recipe, RecipeSearchRequest } from '../../../../core/models';

@Component({
  selector: 'app-recipe-search',
  templateUrl: './recipe-search.component.html',
  styleUrls: ['./recipe-search.component.scss']
})
export class RecipeSearchComponent implements OnInit {
  searchForm: FormGroup;
  recipes: Recipe[] = [];
  filteredRecipes: Recipe[] = [];
  isLoading = false;
  searchPerformed = false;

  // Filter options
  cuisineTypes = [
    'Italian', 'Mexican', 'Asian', 'Mediterranean', 'American', 
    'French', 'Indian', 'Thai', 'Chinese', 'Japanese'
  ];

  dietaryRestrictions = [
    'vegetarian', 'vegan', 'gluten-free', 'dairy-free', 
    'keto', 'paleo', 'low-carb', 'low-sodium'
  ];

  mealTypes = ['breakfast', 'lunch', 'dinner', 'snack', 'dessert'];
  
  preparationTimes = [
    { label: '< 15 min', value: 15 },
    { label: '15-30 min', value: 30 },
    { label: '30-60 min', value: 60 },
    { label: '> 60 min', value: 999 }
  ];

  difficultyLevels = ['easy', 'medium', 'hard'];

  // Selected filters
  selectedCuisines: string[] = [];
  selectedDietary: string[] = [];
  selectedMealTypes: string[] = [];
  selectedDifficulty: string = '';
  selectedPrepTime: number = 0;
  calorieRange = { min: 0, max: 2000 };

  constructor(
    private fb: FormBuilder,
    private recipeService: RecipeService
  ) {
    this.searchForm = this.fb.group({
      searchTerm: [''],
      sortBy: ['relevance']
    });
  }

  ngOnInit(): void {
    this.setupSearch();
    this.loadPopularRecipes();
  }

  private setupSearch(): void {
    this.searchForm.get('searchTerm')?.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap(term => {
          if (term.trim().length > 2) {
            return this.performSearch(term);
          }
          return of([]);
        })
      )
      .subscribe(recipes => {
        this.recipes = recipes;
        this.applyFilters();
      });
  }

  private performSearch(searchTerm: string): Observable<Recipe[]> {
    this.isLoading = true;
    
    const searchRequest: RecipeSearchRequest = {
      filters: {
        query: searchTerm,
        cuisine: this.selectedCuisines,
        dietaryRestrictions: this.selectedDietary,
        maxPrepTime: this.selectedPrepTime > 0 ? this.selectedPrepTime : undefined,
        difficulty: this.selectedDifficulty ? [this.selectedDifficulty as any] : undefined,
        maxCalories: this.calorieRange.max,
        tags: this.selectedMealTypes
      },
      sortBy: this.searchForm.get('sortBy')?.value || 'relevance'
    };

    return this.recipeService.searchRecipes(searchRequest).pipe(
      switchMap(response => {
        this.isLoading = false;
        this.searchPerformed = true;
        return of(response.items);
      })
    );
  }

  private loadPopularRecipes(): void {
    this.isLoading = true;
    this.recipeService.getPopularRecipes(12).subscribe({
      next: (recipes) => {
        this.recipes = recipes;
        this.filteredRecipes = recipes;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading popular recipes:', error);
        this.isLoading = false;
      }
    });
  }

  onFilterChange(): void {
    const searchTerm = this.searchForm.get('searchTerm')?.value || '';
    if (searchTerm.trim().length > 2) {
      this.performSearch(searchTerm).subscribe(recipes => {
        this.recipes = recipes;
        this.applyFilters();
      });
    } else {
      this.applyFilters();
    }
  }

  private applyFilters(): void {
    let filtered = [...this.recipes];

    // Apply client-side filters for immediate feedback
    if (this.selectedCuisines.length > 0) {
      filtered = filtered.filter(recipe => 
        this.selectedCuisines.some(cuisine => 
          recipe.cuisine?.toLowerCase().includes(cuisine.toLowerCase())
        )
      );
    }

    if (this.selectedDietary.length > 0) {
      filtered = filtered.filter(recipe =>
        this.selectedDietary.every(dietary =>
          recipe.tags?.some((tag: string) =>
            tag.toLowerCase().includes(dietary.toLowerCase())
          )
        )
      );
    }

    if (this.selectedMealTypes.length > 0) {
      filtered = filtered.filter(recipe =>
        this.selectedMealTypes.some(mealType =>
          recipe.tags?.some((tag: string) =>
            tag.toLowerCase().includes(mealType.toLowerCase())
          )
        )
      );
    }

    this.filteredRecipes = filtered;
  }

  onCuisineToggle(cuisine: string): void {
    const index = this.selectedCuisines.indexOf(cuisine);
    if (index > -1) {
      this.selectedCuisines.splice(index, 1);
    } else {
      this.selectedCuisines.push(cuisine);
    }
    this.onFilterChange();
  }

  onDietaryToggle(dietary: string): void {
    const index = this.selectedDietary.indexOf(dietary);
    if (index > -1) {
      this.selectedDietary.splice(index, 1);
    } else {
      this.selectedDietary.push(dietary);
    }
    this.onFilterChange();
  }

  onMealTypeToggle(mealType: string): void {
    const index = this.selectedMealTypes.indexOf(mealType);
    if (index > -1) {
      this.selectedMealTypes.splice(index, 1);
    } else {
      this.selectedMealTypes.push(mealType);
    }
    this.onFilterChange();
  }

  onSortChange(): void {
    const sortBy = this.searchForm.get('sortBy')?.value;
    
    switch (sortBy) {
      case 'name':
        this.filteredRecipes.sort((a, b) => a.title.localeCompare(b.title));
        break;
      case 'prepTime':
        this.filteredRecipes.sort((a, b) => (a.prepTimeMinutes || 0) - (b.prepTimeMinutes || 0));
        break;
      case 'calories':
        this.filteredRecipes.sort((a, b) => 
          (a.nutrition?.calories || 0) - (b.nutrition?.calories || 0)
        );
        break;
      case 'rating':
        this.filteredRecipes.sort((a, b) => (b.rating || 0) - (a.rating || 0));
        break;
      default:
        // Keep current order for relevance
        break;
    }
  }

  clearFilters(): void {
    this.selectedCuisines = [];
    this.selectedDietary = [];
    this.selectedMealTypes = [];
    this.selectedDifficulty = '';
    this.selectedPrepTime = 0;
    this.calorieRange = { min: 0, max: 2000 };
    this.searchForm.get('searchTerm')?.setValue('');
    this.loadPopularRecipes();
  }

  viewRecipeDetail(recipeId: string): void {
    // Navigation will be implemented when routing is complete
    console.log('Navigate to recipe detail:', recipeId);
  }

  toggleFavorite(recipe: Recipe): void {
    // Will implement favorite functionality
    console.log('Toggle favorite for recipe:', recipe.id);
  }

  addToMealPlan(recipe: Recipe): void {
    // Will implement meal plan functionality
    console.log('Add to meal plan:', recipe.id);
  }
}
