import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { MealPlanService } from '../../core/services/meal-plan.service';
import { RecipeDetailDialogComponent } from './recipe-detail-dialog';
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
    MatProgressBarModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  private mealPlanService = inject(MealPlanService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  // Signals for reactive state management
  loading = signal(false);
  availableWeeks = signal<WeekViewData[]>([]);
  selectedWeekIndex = signal(0);
  currentMealPlan = signal<MealPlan | null>(null);
  weekMealData = signal<DayMealData[]>([]);
  error = signal<string | null>(null);

  // Computed signal to determine if we have meal plan data
  get hasMealPlan(): boolean {
    const mealPlan = this.currentMealPlan();
    const weekData = this.weekMealData();
    const hasData = !!(mealPlan && weekData && weekData.length > 0);
    
    console.log('🎯 hasMealPlan check:', {
      mealPlan: mealPlan ? 'exists' : 'null',
      weekDataLength: weekData?.length || 0,
      hasData
    });
    
    return hasData;
  }

  // Status enum for template
  MealPlanStatus = MealPlanStatus;

  ngOnInit() {
    console.log('🔧 DashboardComponent initialized');
    
    // Make test methods available in browser console for debugging
    (window as any).debugDashboard = {
      testApiConnection: () => this.testApiConnection(),
      testGenerateMealPlan: () => this.testGenerateMealPlan(),
      getCurrentState: () => ({
        loading: this.loading(),
        error: this.error(),
        currentMealPlan: this.currentMealPlan(),
        weekMealData: this.weekMealData(),
        availableWeeks: this.availableWeeks(),
        selectedWeekIndex: this.selectedWeekIndex()
      })
    };
    
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
    console.log('🔍 Loading meal plan for week index:', weekIndex);
    this.loading.set(true);
    this.error.set(null);
    this.selectedWeekIndex.set(weekIndex);

    try {
      const selectedWeek = this.availableWeeks()[weekIndex];
      if (!selectedWeek) {
        console.log('❌ No selected week found for index:', weekIndex);
        return;
      }

      console.log('📅 Selected week:', selectedWeek);

      // Get meal plans for the selected week
      this.mealPlanService.getMealPlansForWeek(selectedWeek.weekIdentifier).subscribe({
        next: (mealPlans) => {
          console.log('📊 Received meal plans:', mealPlans);
          console.log('📊 Meal plans type:', typeof mealPlans);
          console.log('📊 Is array:', Array.isArray(mealPlans));
          console.log('📊 Length:', mealPlans?.length);
          
          if (mealPlans && mealPlans.length > 0) {
            const activePlan = mealPlans.find(plan => plan.isActive) || mealPlans[0];
            console.log('✅ Active meal plan found:', activePlan);
            console.log('✅ Meal plan type:', typeof activePlan);
            console.log('✅ Meal plan properties:', Object.keys(activePlan || {}));
            
            this.currentMealPlan.set(activePlan);
            this.processMealPlanData(activePlan, selectedWeek);
          } else {
            console.log('⚠️ No meal plans found for this week');
            this.currentMealPlan.set(null);
            this.weekMealData.set([]);
            // Clear any previous meal data
            this.clearMealData();
          }
          this.loading.set(false);
        },
        error: (error) => {
          console.error('❌ Error loading meal plan:', error);
          this.error.set('Failed to load meal plan for this week');
          this.loading.set(false);
        }
      });
    } catch (error) {
      console.error('❌ Error in loadWeekMealPlan:', error);
      this.error.set('An unexpected error occurred');
      this.loading.set(false);
    }
  }

  private processMealPlanData(mealPlan: MealPlan, weekData: WeekViewData) {
    console.log('🔄 Processing meal plan data:', mealPlan);
    console.log('🔄 Week data:', weekData);
    
    const daysOfWeek = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
    const dayMealData: DayMealData[] = [];

    daysOfWeek.forEach((dayName, index) => {
      const currentDate = new Date(weekData.weekStartDate);
      currentDate.setDate(weekData.weekStartDate.getDate() + index);
      
      const dayKey = dayName.toLowerCase();
      console.log(`🔄 Processing day: ${dayName} (${dayKey})`);
      
      // Handle both camelCase and PascalCase API responses
      const dailyMeals = mealPlan.dailyMeals || (mealPlan as any).DailyMeals;
      const dailyPlan = dailyMeals?.[dayKey];
      console.log(`🔄 Daily plan for ${dayName}:`, dailyPlan);

      if (dailyPlan) {
        // Handle both camelCase and PascalCase meal properties
        const meals = dailyPlan.meals || (dailyPlan as any).Meals;
        const nutrition = dailyPlan.dailyNutrition || (dailyPlan as any).DailyNutrition;
        
        console.log(`🔄 Meals for ${dayName}:`, meals);
        console.log(`🔄 Nutrition for ${dayName}:`, nutrition);
        
        const dayData = {
          dayName,
          date: currentDate,
          breakfast: meals?.['breakfast'] || meals?.['Breakfast'] || undefined,
          lunch: meals?.['lunch'] || meals?.['Lunch'] || undefined,
          dinner: meals?.['dinner'] || meals?.['Dinner'] || undefined,
          snacks: dailyPlan.snacks || (dailyPlan as any).Snacks || [],
          dailyNutrition: {
            totalCalories: (nutrition as any)?.TotalCalories || nutrition?.totalCalories || 0,
            totalProtein: (nutrition as any)?.TotalProtein || nutrition?.totalProtein || 0,
            totalCarbs: (nutrition as any)?.TotalCarbs || nutrition?.totalCarbs || 0,
            totalFat: (nutrition as any)?.TotalFats || nutrition?.totalFat || 0,
            totalFiber: (nutrition as any)?.TotalFiber || nutrition?.totalFiber || 0,
            totalSugar: (nutrition as any)?.TotalSugar || nutrition?.totalSugar || 0,
            totalSodium: (nutrition as any)?.TotalSodium || nutrition?.totalSodium || 0,
            averageCaloriesPerDay: (nutrition as any)?.AverageDailyCalories || nutrition?.averageCaloriesPerDay || 0
          },
          dailyEstimatedCost: dailyPlan.dailyEstimatedCost || (dailyPlan as any).DailyEstimatedCost || 0
        };
        
        console.log(`🔄 Final breakfast data for ${dayName}:`, dayData.breakfast);
        console.log(`🔄 Final lunch data for ${dayName}:`, dayData.lunch);
        console.log(`🔄 Final dinner data for ${dayName}:`, dayData.dinner);
        
        // Validate meal data
        if (dayData.breakfast && (!dayData.breakfast.name || dayData.breakfast.name.length < 3)) {
          console.warn(`⚠️ Invalid breakfast name for ${dayName}:`, dayData.breakfast.name);
        }
        if (dayData.lunch && (!dayData.lunch.name || dayData.lunch.name.length < 3)) {
          console.warn(`⚠️ Invalid lunch name for ${dayName}:`, dayData.lunch.name);
        }
        if (dayData.dinner && (!dayData.dinner.name || dayData.dinner.name.length < 3)) {
          console.warn(`⚠️ Invalid dinner name for ${dayName}:`, dayData.dinner.name);
        }
        console.log(`✅ Day data for ${dayName}:`, dayData);
        dayMealData.push(dayData);
      } else {
        // No meal plan for this day
        const emptyDayData = {
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
        };
        console.log(`⚠️ No meal plan for ${dayName}, using empty data:`, emptyDayData);
        dayMealData.push(emptyDayData);
      }
    });

    console.log('🎯 Final processed meal data:', dayMealData);
    this.weekMealData.set(dayMealData);
    console.log('🎯 WeekMealData signal updated. Current value:', this.weekMealData());
  }

  async generateMealPlan() {
    const selectedWeek = this.availableWeeks()[this.selectedWeekIndex()];
    if (!selectedWeek) return;

    console.log('🚀 Starting meal plan generation for week:', selectedWeek);
    this.loading.set(true);
    try {
      this.mealPlanService.generateMealPlan({
        weekIdentifier: selectedWeek.weekIdentifier,
        regenerateExisting: false
      }).subscribe({
        next: (response) => {
          console.log('✅ GenerateMealPlan response received:', response);
          if (response && response.orchestrationId) {
            this.snackBar.open('Meal plan generation started! This may take a few minutes.', 'OK', {
              duration: 5000
            });
            
            // Poll for completion
            this.pollGenerationStatus(response.orchestrationId);
          } else {
            console.error('❌ Invalid response structure - no orchestrationId:', response);
            this.snackBar.open('Invalid response from server', 'OK', {
              duration: 3000
            });
            this.loading.set(false);
          }
        },
        error: (error) => {
          console.error('❌ Error generating meal plan:', error);
          this.snackBar.open('Failed to start meal plan generation', 'OK', {
            duration: 3000
          });
          this.loading.set(false);
        }
      });
    } catch (error) {
      console.error('❌ Error in generateMealPlan:', error);
      this.loading.set(false);
    }
  }

  async regenerateMealPlan() {
    const selectedWeek = this.availableWeeks()[this.selectedWeekIndex()];
    if (!selectedWeek) return;

    const confirmed = confirm('Are you sure you want to regenerate the meal plan for this week? This will replace your current meal plan.');
    if (!confirmed) return;

    console.log('🔄 Regenerating meal plan for week:', selectedWeek);
    this.loading.set(true);
    try {
      this.mealPlanService.generateMealPlan({
        weekIdentifier: selectedWeek.weekIdentifier,
        regenerateExisting: true
      }).subscribe({
        next: (response) => {
          console.log('✅ Regenerate response received:', response);
          if (response && response.orchestrationId) {
            this.snackBar.open('Meal plan regeneration started! This may take a few minutes.', 'OK', {
              duration: 5000
            });
            
            // Poll for completion
            this.pollGenerationStatus(response.orchestrationId);
          } else {
            console.error('❌ Invalid response structure - no orchestrationId:', response);
            this.snackBar.open('Invalid response from server', 'OK', {
              duration: 3000
            });
            this.loading.set(false);
          }
        },
        error: (error) => {
          console.error('❌ Error regenerating meal plan:', error);
          this.snackBar.open('Failed to regenerate meal plan', 'OK', {
            duration: 3000
          });
          this.loading.set(false);
        }
      });
    } catch (error) {
      console.error('❌ Error in regenerateMealPlan:', error);
      this.loading.set(false);
    }
  }

  private pollGenerationStatus(orchestrationId: string) {
    console.log('🔄 Starting to poll generation status for:', orchestrationId);
    const pollInterval = setInterval(() => {
      this.mealPlanService.getMealPlanStatus(orchestrationId).subscribe({
        next: (status) => {
          console.log('📊 Poll status response:', status);
          if (status.status === 'Completed') {
            console.log('✅ Meal plan generation completed!');
            clearInterval(pollInterval);
            this.snackBar.open('Meal plan generated successfully!', 'OK', {
              duration: 3000
            });
            // Reload the current week's data
            this.loadWeekMealPlan(this.selectedWeekIndex());
          } else if (status.status === 'Failed') {
            console.log('❌ Meal plan generation failed:', status);
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

  getStatusColor(status: MealPlanStatus | string): string {
    const statusString = typeof status === 'string' ? status : String(status);
    switch (statusString.toLowerCase()) {
      case 'generated':
      case 'completed':
        return 'primary';
      case 'inprogress':
      case 'generating':
        return 'accent';
      case 'failed':
      case 'cancelled':
        return 'warn';
      default:
        return 'accent';
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

  getDayIcon(dayName: string): string {
    const icons: { [key: string]: string } = {
      monday: 'today',
      tuesday: 'event',
      wednesday: 'schedule',
      thursday: 'calendar_today',
      friday: 'weekend',
      saturday: 'celebration',
      sunday: 'self_care'
    };
    return icons[dayName.toLowerCase()] || 'today';
  }

  // Helper methods for template to access dynamic properties safely
  getMealPlanStatus(mealPlan: any): string {
    if (!mealPlan) return 'No Plan';
    if ((mealPlan as any)?.Status === 1) {
      return 'Generated';
    }
    return (mealPlan as any)?.status || (mealPlan as any)?.Status || 'Unknown';
  }

  getMealPlanStatusForColor(mealPlan: any): string {
    return this.getMealPlanStatus(mealPlan);
  }

  getMealPlanStatusIcon(mealPlan: any): string {
    if (!mealPlan) return 'schedule';
    const status = this.getMealPlanStatus(mealPlan);
    return status === 'Generated' ? 'check_circle' : 'schedule';
  }

  getTotalCost(mealPlan: any): number {
    if (!mealPlan) return 0;
    return (mealPlan as any)?.TotalEstimatedCost || (mealPlan as any)?.totalEstimatedCost || 0;
  }

  getAverageCalories(mealPlan: any): number {
    if (!mealPlan) return 0;
    const weeklyNutrition = (mealPlan as any)?.WeeklyNutritionSummary || (mealPlan as any)?.weeklyNutritionSummary;
    if (!weeklyNutrition) return 0;
    return weeklyNutrition?.AverageDailyCalories || weeklyNutrition?.averageCaloriesPerDay || 0;
  }

  getTotalProtein(mealPlan: any): number {
    if (!mealPlan) return 0;
    const weeklyNutrition = (mealPlan as any)?.WeeklyNutritionSummary || (mealPlan as any)?.weeklyNutritionSummary;
    if (!weeklyNutrition) return 0;
    return weeklyNutrition?.TotalProtein || weeklyNutrition?.totalProtein || 0;
  }

  clearAllData(): void {
    console.log('🧹 Clearing all dashboard data');
    this.currentMealPlan.set(null);
    this.weekMealData.set([]);
    this.error.set(null);
    this.loading.set(false);
    this.snackBar.open('All data cleared successfully', 'Close', { duration: 3000 });
  }

  private clearMealData() {
    console.log('🧹 Clearing meal data');
    this.weekMealData.set([]);
    this.currentMealPlan.set(null);
  }

  isToday(date: string | Date): boolean {
    const today = new Date();
    const checkDate = typeof date === 'string' ? new Date(date) : date;
    return today.toDateString() === checkDate.toDateString();
  }

  viewRecipe(meal: any): void {
    console.log('Opening recipe dialog for:', meal);
    
    const dialogRef = this.dialog.open(RecipeDetailDialogComponent, {
      data: meal,
      width: '800px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      autoFocus: false,
      restoreFocus: false
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        console.log('Dialog result:', result);
      }
    });
  }

  // Debug methods for browser console testing
  testApiConnection(): void {
    console.log('🧪 Testing API connection...');
    const headers = new Headers({
      'Content-Type': 'application/json'
    });
    
    fetch('http://localhost:7066/api/mealplans?week=2025-W37', { headers })
      .then(response => {
        console.log('✅ API Response status:', response.status);
        return response.json();
      })
      .then(data => {
        console.log('✅ API Response data:', data);
      })
      .catch(error => {
        console.error('❌ API Connection failed:', error);
      });
  }

  testGenerateMealPlan(): void {
    console.log('🧪 Testing meal plan generation...');
    const selectedWeek = this.availableWeeks()[0];
    if (selectedWeek) {
      this.generateMealPlan();
    } else {
      console.error('❌ No week available for testing');
    }
  }
}
