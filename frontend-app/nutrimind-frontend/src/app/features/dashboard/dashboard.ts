import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MealPlanService } from '../../core/services/meal-plan.service';
import { HeaderComponent } from '../../shared/components/header.component';
import { 
  MealPlan, 
  WeekViewData, 
  DayMealData, 
  Meal,
  MealPlanStatus 
} from '../../shared/models/meal-plan.models';

@Component({
  selector: 'app-dashboard',
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatDividerModule,
    HeaderComponent
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  private mealPlanService = inject(MealPlanService);
  private snackBar = inject(MatSnackBar);

  // Signals for reactive state management
  loading = signal(false);
  availableWeeks = signal<WeekViewData[]>([]);
  selectedWeekIndex = signal(0);
  currentMealPlan = signal<MealPlan | null>(null);
  weekMealData = signal<DayMealData[]>([]);
  error = signal<string | null>(null);

  // Status enum for template
  MealPlanStatus = MealPlanStatus;

  ngOnInit() {
    this.initializeDashboard();
  }

  private async initializeDashboard() {
    try {
      // Get available weeks
      const weeks = this.mealPlanService.getUpcomingWeeks(4);
      this.availableWeeks.set(weeks);
      
      // Load current week's meal plan
      await this.loadWeekMealPlan(0);
    } catch (error) {
      console.error('Error initializing dashboard:', error);
      this.error.set('Failed to load dashboard data');
    }
  }

  async loadWeekMealPlan(weekIndex: number) {
    this.loading.set(true);
    this.error.set(null);
    this.selectedWeekIndex.set(weekIndex);

    try {
      const selectedWeek = this.availableWeeks()[weekIndex];
      if (!selectedWeek) return;

      // Get meal plans for the selected week
      this.mealPlanService.getMealPlansForWeek(selectedWeek.weekIdentifier).subscribe({
        next: (mealPlans) => {
          if (mealPlans && mealPlans.length > 0) {
            const activePlan = mealPlans.find(plan => plan.isActive) || mealPlans[0];
            this.currentMealPlan.set(activePlan);
            this.processMealPlanData(activePlan, selectedWeek);
          } else {
            this.currentMealPlan.set(null);
            this.weekMealData.set([]);
          }
          this.loading.set(false);
        },
        error: (error) => {
          console.error('Error loading meal plan:', error);
          this.error.set('Failed to load meal plan for this week');
          this.loading.set(false);
        }
      });
    } catch (error) {
      console.error('Error in loadWeekMealPlan:', error);
      this.error.set('An unexpected error occurred');
      this.loading.set(false);
    }
  }

  private processMealPlanData(mealPlan: MealPlan, weekData: WeekViewData) {
    const daysOfWeek = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
    const dayMealData: DayMealData[] = [];

    daysOfWeek.forEach((dayName, index) => {
      const currentDate = new Date(weekData.weekStartDate);
      currentDate.setDate(weekData.weekStartDate.getDate() + index);
      
      const dayKey = dayName.toLowerCase();
      const dailyPlan = mealPlan.dailyMeals[dayKey];

      if (dailyPlan) {
        dayMealData.push({
          dayName,
          date: currentDate,
          breakfast: dailyPlan.meals['breakfast'],
          lunch: dailyPlan.meals['lunch'],
          dinner: dailyPlan.meals['dinner'],
          snacks: dailyPlan.snacks || [],
          dailyNutrition: dailyPlan.dailyNutrition,
          dailyEstimatedCost: dailyPlan.dailyEstimatedCost
        });
      } else {
        // No meal plan for this day
        dayMealData.push({
          dayName,
          date: currentDate,
          snacks: [],
          dailyNutrition: {
            totalCalories: 0,
            totalProtein: 0,
            totalCarbs: 0,
            totalFat: 0,
            totalFiber: 0,
            totalSugar: 0,
            totalSodium: 0,
            averageCaloriesPerDay: 0
          },
          dailyEstimatedCost: 0
        });
      }
    });

    this.weekMealData.set(dayMealData);
  }

  async generateMealPlan() {
    const selectedWeek = this.availableWeeks()[this.selectedWeekIndex()];
    if (!selectedWeek) return;

    this.loading.set(true);
    try {
      this.mealPlanService.generateMealPlan({
        weekIdentifier: selectedWeek.weekIdentifier,
        regenerateExisting: false
      }).subscribe({
        next: (response) => {
          this.snackBar.open('Meal plan generation started! This may take a few minutes.', 'OK', {
            duration: 5000
          });
          
          // Poll for completion
          this.pollGenerationStatus(response.orchestrationId);
        },
        error: (error) => {
          console.error('Error generating meal plan:', error);
          this.snackBar.open('Failed to start meal plan generation', 'OK', {
            duration: 3000
          });
          this.loading.set(false);
        }
      });
    } catch (error) {
      console.error('Error in generateMealPlan:', error);
      this.loading.set(false);
    }
  }

  private pollGenerationStatus(orchestrationId: string) {
    const pollInterval = setInterval(() => {
      this.mealPlanService.getMealPlanStatus(orchestrationId).subscribe({
        next: (status) => {
          if (status.status === 'Completed') {
            clearInterval(pollInterval);
            this.snackBar.open('Meal plan generated successfully!', 'OK', {
              duration: 3000
            });
            // Reload the current week's data
            this.loadWeekMealPlan(this.selectedWeekIndex());
          } else if (status.status === 'Failed') {
            clearInterval(pollInterval);
            this.snackBar.open('Meal plan generation failed. Please try again.', 'OK', {
              duration: 5000
            });
            this.loading.set(false);
          }
        },
        error: (error) => {
          console.error('Error checking generation status:', error);
          clearInterval(pollInterval);
          this.loading.set(false);
        }
      });
    }, 5000); // Poll every 5 seconds

    // Stop polling after 5 minutes
    setTimeout(() => {
      clearInterval(pollInterval);
      if (this.loading()) {
        this.loading.set(false);
        this.snackBar.open('Generation is taking longer than expected. Please refresh to check status.', 'OK', {
          duration: 5000
        });
      }
    }, 300000);
  }

  getStatusColor(status: MealPlanStatus): string {
    switch (status) {
      case MealPlanStatus.Generated:
      case MealPlanStatus.Completed:
        return 'primary';
      case MealPlanStatus.InProgress:
        return 'accent';
      case MealPlanStatus.Generating:
        return 'warn';
      case MealPlanStatus.Failed:
      case MealPlanStatus.Cancelled:
        return '';
      default:
        return '';
    }
  }

  getMealTypeIcon(mealType: string): string {
    switch (mealType.toLowerCase()) {
      case 'breakfast':
        return 'free_breakfast';
      case 'lunch':
        return 'lunch_dining';
      case 'dinner':
        return 'dinner_dining';
      case 'snack':
        return 'cookie';
      default:
        return 'restaurant';
    }
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  formatTime(minutes: number): string {
    if (minutes < 60) {
      return `${minutes}m`;
    }
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
  }
}
