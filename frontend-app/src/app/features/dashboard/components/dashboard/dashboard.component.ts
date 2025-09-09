import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../../../core/services/auth.service';
import { UserService } from '../../../../core/services/user.service';
import { RecipeService } from '../../../../core/services/recipe.service';
import { User, Recipe, UserPreferences } from '../../../../core/models';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  currentUser: User | null = null;
  userPreferences: UserPreferences | null = null;
  recommendedRecipes: Recipe[] = [];
  popularRecipes: Recipe[] = [];
  isLoading = true;
  dashboardStats = {
    totalMealPlans: 5,
    completedGoals: 12,
    recipesCooked: 28,
    caloriesSaved: 2450
  };

  nutritionProgress = {
    calories: { current: 1680, target: 2000, percentage: 84 },
    protein: { current: 95, target: 120, percentage: 79 },
    carbs: { current: 180, target: 250, percentage: 72 },
    fat: { current: 55, target: 65, percentage: 85 }
  };

  recentActivity = [
    { action: 'Completed meal plan', item: 'Mediterranean Week', time: '2 hours ago', icon: 'check_circle' },
    { action: 'Added recipe', item: 'Quinoa Power Bowl', time: '5 hours ago', icon: 'restaurant' },
    { action: 'Updated preferences', item: 'Dietary restrictions', time: '1 day ago', icon: 'settings' },
    { action: 'Grocery shopping', item: 'Weekly essentials', time: '2 days ago', icon: 'shopping_cart' }
  ];

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private recipeService: RecipeService
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        this.loadUserPreferences(user.id);
        this.loadRecommendedRecipes(user.id);
      }
    });

    this.loadPopularRecipes();
  }

  private loadUserPreferences(userId: string): void {
    this.userService.getUserPreferences(userId).subscribe({
      next: (preferences) => {
        this.userPreferences = preferences;
      },
      error: (error) => {
        console.error('Error loading user preferences:', error);
      }
    });
  }

  private loadRecommendedRecipes(userId: string): void {
    this.recipeService.generateRecipeRecommendations(userId).subscribe({
      next: (recipes) => {
        this.recommendedRecipes = recipes.slice(0, 6);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading recommended recipes:', error);
        this.isLoading = false;
      }
    });
  }

  private loadPopularRecipes(): void {
    this.recipeService.getPopularRecipes(6).subscribe({
      next: (recipes) => {
        this.popularRecipes = recipes;
      },
      error: (error) => {
        console.error('Error loading popular recipes:', error);
      }
    });
  }

  getGreeting(): string {
    const hour = new Date().getHours();
    const name = this.currentUser?.firstName || 'there';
    
    if (hour < 12) return `Good morning, ${name}!`;
    if (hour < 18) return `Good afternoon, ${name}!`;
    return `Good evening, ${name}!`;
  }

  getMotivationalMessage(): string {
    const messages = [
      "You're doing great on your healthy journey! 🌱",
      "Every healthy choice counts! Keep it up! 💪",
      "Your future self will thank you! 🌟",
      "Progress, not perfection! 🎯",
      "Healthy habits are building! 🚀"
    ];
    return messages[Math.floor(Math.random() * messages.length)];
  }

  getNutritionColor(percentage: number): string {
    if (percentage >= 90) return '#4CAF50';
    if (percentage >= 70) return '#FF9800';
    return '#f44336';
  }

  navigateToMealPlans(): void {
    // Will be implemented when meal plans component is created
  }

  navigateToRecipes(): void {
    // Will be implemented when recipes component is created
  }

  navigateToProfile(): void {
    // Will be implemented when profile component is created
  }
}
