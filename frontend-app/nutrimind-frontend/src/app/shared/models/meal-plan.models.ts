// Meal plan related models (reconstructed; adjust to match backend contracts as needed)

export interface NutritionalInfo {
	calories: number;
	protein: number; // grams
	carbs: number;   // grams
	fat: number;     // grams
	fiber?: number;
	sugar?: number;
}

export interface Ingredient {
	name: string;
	quantity: string; // e.g. '2 cups', '150g'
	notes?: string;
}

export interface RecipeStep {
	order: number;
	instruction: string;
}

export interface RecipeSummary {
	id?: string;
	title: string;
	description?: string;
	ingredients: Ingredient[];
	steps: RecipeStep[];
	nutrition: NutritionalInfo;
	prepTimeMinutes?: number;
	cookTimeMinutes?: number;
	totalTimeMinutes?: number;
	imageUrl?: string;
	tags?: string[];
}

export interface MealEntry {
	mealType: 'Breakfast' | 'Lunch' | 'Dinner' | 'Snack';
	recipe: RecipeSummary;
	scheduledTime?: string; // ISO time or datetime
}

export interface DailyMealPlan {
	date: string; // ISO date
	meals: MealEntry[];
	totalNutrition: NutritionalInfo;
}



// ===== Additional types inferred from dashboard & service usage =====

export enum MealPlanStatus {
	Pending = 'Pending',
	Generating = 'Generating',
	InProgress = 'InProgress',
	Generated = 'Generated',
	Completed = 'Completed',
	Failed = 'Failed',
	Cancelled = 'Cancelled'
}

export interface GenerateMealPlanRequest {
	weekIdentifier: string; // e.g. 2025-W09
	regenerateExisting?: boolean;
	// Optional tuning parameters
	calorieTarget?: number;
	proteinTarget?: number;
}

export interface MealPlanGenerationResponse {
	orchestrationId: string;
	status: MealPlanStatus | 'Started' | 'Queued';
	message?: string;
}

export interface WeekViewData {
	weekIdentifier: string;
	weekStartDate: Date;
	weekEndDate: Date;
	displayText: string;
}

// Dashboard expects mealPlan.dailyMeals[dayKey] with structure below
export interface DayMealDataMap {
	[dayKey: string]: DailyMealPlanDetail; // monday, tuesday etc.
}

export interface DailyMealPlanDetail {
	meals: { [mealType: string]: Meal | undefined }; // breakfast/lunch/dinner
	snacks?: Meal[];
	dailyNutrition: DailyNutritionSummary;
	dailyEstimatedCost: number;
}

export interface DailyNutritionSummary {
	totalCalories: number;
	totalProtein: number;
	totalCarbs: number;
	totalFat: number;
	totalFiber: number;
	totalSugar: number;
	totalSodium: number;
	averageCaloriesPerDay?: number; // used in fallback
}

export interface MealNutritionInfo {
	calories: number;
	protein: number;
	carbs: number;
	fat: number;
	fiber?: number;
	sugar?: number;
	sodium?: number;
}

export interface MealRecipeMeta {
	cuisine?: string;
	course?: string;
	difficulty?: string;
	tags?: string[];
}

export interface Meal {
	id: string;
	name: string;
	description?: string;
	nutrition: MealNutritionInfo;
	estimatedPrepTime: number; // minutes
	estimatedCookTime: number; // minutes
	recipe: MealRecipeMeta & { id?: string };
}

export interface DayMealData {
	dayName: string; // Monday etc.
	date: Date;
	breakfast?: Meal;
	lunch?: Meal;
	dinner?: Meal;
	snacks: Meal[];
	dailyNutrition: DailyNutritionSummary;
	dailyEstimatedCost: number;
}

export interface GroceryListItem {
	id: string;
	name: string;
	quantity: number;
	unit: string;
	estimatedCost: number;
	category?: string;
}

export interface GroceryListSummary {
	items: GroceryListItem[];
	totalEstimatedCost: number;
}

// Extend MealPlan with UI-specific aggregated fields referenced in templates
export interface MealPlan extends MealPlanBase {
	// UI aggregated values
	totalEstimatedCost: number;
	weeklyNutritionSummary: {
		averageCaloriesPerDay: number;
		totalProtein: number;
	};
	status: MealPlanStatus;
	isActive: boolean;
	dailyMeals: DayMealDataMap; // dictionary form used in dashboard
	groceryList?: GroceryListSummary;
}

// Separate base to avoid circular re-definition (internal)
interface MealPlanBase {
	id: string;
	userId: string;
	createdAt: string;
	generatedForRange: { start: string; end: string };
	days?: DailyMealPlan[]; // may be absent if using dailyMeals map form
	aggregateNutrition?: NutritionalInfo;
}


