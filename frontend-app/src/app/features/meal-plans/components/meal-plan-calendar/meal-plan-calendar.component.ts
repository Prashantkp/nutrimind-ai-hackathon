import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { startOfWeek, endOfWeek, eachDayOfInterval, format, addWeeks, subWeeks } from 'date-fns';
import { AuthService } from '../../../../core/services/auth.service';
import { MealPlan, PlannedMeal, MealType, User } from '../../../../core/models';

@Component({
  selector: 'app-meal-plan-calendar',
  templateUrl: './meal-plan-calendar.component.html',
  styleUrls: ['./meal-plan-calendar.component.scss']
})
export class MealPlanCalendarComponent implements OnInit {
  currentUser: User | null = null;
  currentWeek: Date = new Date();
  weekDays: Date[] = [];
  activeMealPlan: MealPlan | null = null;
  plannedMeals: PlannedMeal[] = [];
  isLoading = false;

  mealTypes: MealType[] = [
    MealType.Breakfast,
    MealType.Lunch,
    MealType.Dinner,
    MealType.Snack
  ];

  mealTypeColors = {
    [MealType.Breakfast]: '#FFF3E0',
    [MealType.Lunch]: '#E8F5E8',
    [MealType.Dinner]: '#E3F2FD',
    [MealType.Snack]: '#F3E5F5'
  };

  mealTypeIcons = {
    [MealType.Breakfast]: 'free_breakfast',
    [MealType.Lunch]: 'lunch_dining',
    [MealType.Dinner]: 'dinner_dining',
    [MealType.Snack]: 'local_cafe'
  };

  constructor(
    private authService: AuthService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        this.loadMealPlan();
      }
    });
    this.generateWeekDays();
  }

  private generateWeekDays(): void {
    const weekStart = startOfWeek(this.currentWeek, { weekStartsOn: 1 }); // Monday start
    const weekEnd = endOfWeek(this.currentWeek, { weekStartsOn: 1 });
    this.weekDays = eachDayOfInterval({ start: weekStart, end: weekEnd });
  }

  private loadMealPlan(): void {
    if (!this.currentUser) return;
    
    this.isLoading = true;
    
    // Mock data - replace with actual API call
    setTimeout(() => {
      this.activeMealPlan = {
        id: '1',
        userId: this.currentUser!.id,
        name: 'Healthy Week Plan',
        description: 'Balanced nutrition for the week',
        startDate: startOfWeek(this.currentWeek, { weekStartsOn: 1 }),
        endDate: endOfWeek(this.currentWeek, { weekStartsOn: 1 }),
        isActive: true,
        meals: this.generateMockMeals(),
        totalNutrition: {
          calories: 1800,
          protein: 120,
          carbs: 200,
          fat: 60,
          fiber: 35,
          sugar: 50,
          sodium: 2000
        },
        createdAt: new Date(),
        updatedAt: new Date()
      };
      
      this.plannedMeals = this.activeMealPlan.meals;
      this.isLoading = false;
    }, 1000);
  }

  private generateMockMeals(): PlannedMeal[] {
    const meals: PlannedMeal[] = [];
    
    this.weekDays.forEach((day, dayIndex) => {
      this.mealTypes.forEach((mealType, typeIndex) => {
        if (Math.random() > 0.3) { // 70% chance of having a meal
          meals.push({
            id: `${dayIndex}-${typeIndex}`,
            mealPlanId: '1',
            recipeId: `recipe-${dayIndex}-${typeIndex}`,
            recipe: {
              id: `recipe-${dayIndex}-${typeIndex}`,
              title: this.getMockRecipeTitle(mealType),
              description: 'Delicious and healthy recipe',
              ingredients: [],
              instructions: [],
              nutrition: {
                calories: Math.floor(Math.random() * 500) + 300,
                protein: Math.floor(Math.random() * 30) + 15,
                carbs: Math.floor(Math.random() * 50) + 25,
                fat: Math.floor(Math.random() * 20) + 10
              },
              servings: 2,
              prepTimeMinutes: Math.floor(Math.random() * 30) + 15,
              cookTimeMinutes: Math.floor(Math.random() * 45) + 15,
              totalTimeMinutes: Math.floor(Math.random() * 60) + 30,
              difficulty: 'intermediate' as any,
              cuisine: 'Mediterranean',
              tags: ['healthy', 'quick'],
              rating: Math.floor(Math.random() * 2) + 4,
              createdAt: new Date(),
              updatedAt: new Date()
            },
            date: day,
            mealType: mealType,
            servings: 2,
            nutrition: {
              calories: Math.floor(Math.random() * 500) + 300,
              protein: Math.floor(Math.random() * 30) + 15,
              carbs: Math.floor(Math.random() * 50) + 25,
              fat: Math.floor(Math.random() * 20) + 10
            }
          });
        }
      });
    });
    
    return meals;
  }

  private getMockRecipeTitle(mealType: MealType): string {
    const recipes = {
      [MealType.Breakfast]: [
        'Overnight Oats with Berries',
        'Avocado Toast with Eggs',
        'Greek Yogurt Parfait',
        'Protein Smoothie Bowl',
        'Veggie Scramble'
      ],
      [MealType.Lunch]: [
        'Mediterranean Quinoa Bowl',
        'Grilled Chicken Salad',
        'Vegetarian Wrap',
        'Salmon Power Bowl',
        'Turkey and Hummus Sandwich'
      ],
      [MealType.Dinner]: [
        'Grilled Salmon with Vegetables',
        'Chicken Stir-Fry',
        'Vegetarian Pasta',
        'Beef and Broccoli',
        'Mediterranean Chicken'
      ],
      [MealType.Snack]: [
        'Mixed Nuts and Fruit',
        'Greek Yogurt with Honey',
        'Vegetable Sticks with Hummus',
        'Protein Smoothie',
        'Apple with Almond Butter'
      ]
    };
    
    const options = recipes[mealType];
    return options[Math.floor(Math.random() * options.length)];
  }

  getMealsForDay(day: Date): PlannedMeal[] {
    return this.plannedMeals.filter(meal => 
      format(meal.date, 'yyyy-MM-dd') === format(day, 'yyyy-MM-dd')
    );
  }

  getMealForDayAndType(day: Date, mealType: MealType): PlannedMeal | null {
    return this.plannedMeals.find(meal => 
      format(meal.date, 'yyyy-MM-dd') === format(day, 'yyyy-MM-dd') && 
      meal.mealType === mealType
    ) || null;
  }

  previousWeek(): void {
    this.currentWeek = subWeeks(this.currentWeek, 1);
    this.generateWeekDays();
    this.loadMealPlan();
  }

  nextWeek(): void {
    this.currentWeek = addWeeks(this.currentWeek, 1);
    this.generateWeekDays();
    this.loadMealPlan();
  }

  currentWeekText(): string {
    const start = startOfWeek(this.currentWeek, { weekStartsOn: 1 });
    const end = endOfWeek(this.currentWeek, { weekStartsOn: 1 });
    return `${format(start, 'MMM d')} - ${format(end, 'MMM d, yyyy')}`;
  }

  addMealToSlot(day: Date, mealType: MealType): void {
    console.log('Add meal to slot:', format(day, 'yyyy-MM-dd'), mealType);
    // Will implement meal selection dialog
  }

  editMeal(meal: PlannedMeal): void {
    console.log('Edit meal:', meal.id);
    // Will implement meal editing
  }

  removeMeal(meal: PlannedMeal): void {
    const index = this.plannedMeals.findIndex(m => m.id === meal.id);
    if (index > -1) {
      this.plannedMeals.splice(index, 1);
    }
  }

  generateGroceryList(): void {
    console.log('Generate grocery list for week');
    // Will implement grocery list generation
  }

  createNewMealPlan(): void {
    console.log('Create new meal plan');
    // Will implement meal plan creation
  }

  duplicateWeek(): void {
    console.log('Duplicate current week');
    // Will implement week duplication
  }

  getTotalNutritionForDay(day: Date): { calories: number; protein: number; carbs: number; fat: number } {
    const dayMeals = this.getMealsForDay(day);
    return dayMeals.reduce((total, meal) => ({
      calories: total.calories + (meal.nutrition.calories || 0),
      protein: total.protein + (meal.nutrition.protein || 0),
      carbs: total.carbs + (meal.nutrition.carbs || 0),
      fat: total.fat + (meal.nutrition.fat || 0)
    }), { calories: 0, protein: 0, carbs: 0, fat: 0 });
  }

  // Additional methods for simplified template
  addMeal(day: Date, mealType: MealType): void {
    console.log('Adding meal for', day, mealType);
  }

  goToToday(): void {
    this.currentWeek = new Date();
  }

  getMondayOfWeek(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    return new Date(d.setDate(diff));
  }

  getDateForDay(dayIndex: number): Date {
    const monday = this.getMondayOfWeek(this.currentWeek);
    return new Date(monday.getTime() + (dayIndex * 24 * 60 * 60 * 1000));
  }

  formatDate(date: Date): string {
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }
}
