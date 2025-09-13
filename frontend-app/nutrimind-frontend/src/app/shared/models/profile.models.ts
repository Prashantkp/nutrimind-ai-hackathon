// Profile related enums & interfaces (reconstructed)

export enum ActivityLevel {
	Sedentary = 'Sedentary',
	LightlyActive = 'LightlyActive',
	ModeratelyActive = 'ModeratelyActive',
	VeryActive = 'VeryActive',
	SuperActive = 'SuperActive'
}

export enum DietaryPreference {
	None = 'None',
	Vegetarian = 'Vegetarian',
	Vegan = 'Vegan',
	Pescatarian = 'Pescatarian',
	Keto = 'Keto',
	Paleo = 'Paleo',
	Mediterranean = 'Mediterranean',
	LowCarb = 'LowCarb',
	GlutenFree = 'GlutenFree'
}

export enum Allergen {
	Milk = 'Milk',
	Eggs = 'Eggs',
	Fish = 'Fish',
	Shellfish = 'Shellfish',
	TreeNuts = 'TreeNuts',
	Peanuts = 'Peanuts',
	Wheat = 'Wheat',
	Soybeans = 'Soybeans',
	Sesame = 'Sesame'
}

export enum HealthGoal {
	WeightLoss = 'WeightLoss',
	WeightGain = 'WeightGain',
	MuscleGain = 'MuscleGain',
	ImprovedEnergy = 'ImprovedEnergy',
	BetterDigestion = 'BetterDigestion',
	HeartHealth = 'HeartHealth',
	BloodSugarControl = 'BloodSugarControl',
	ReduceInflammation = 'ReduceInflammation'
}

export enum MedicalCondition {
	Diabetes = 'Diabetes',
	Hypertension = 'Hypertension',
	HeartDisease = 'HeartDisease',
	KidneyDisease = 'KidneyDisease',
	Thyroid = 'Thyroid',
	PCOS = 'PCOS',
	IBD = 'IBD'
}

export enum MealPreference {
	HighProtein = 'HighProtein',
	LowCarb = 'LowCarb',
	Balanced = 'Balanced',
	LowFat = 'LowFat',
	HighFiber = 'HighFiber'
}

export enum CookingSkillLevel {
	Beginner = 'Beginner',
	Intermediate = 'Intermediate',
	Advanced = 'Advanced'
}

export enum BudgetRange {
	Low = 'Low',
	Medium = 'Medium',
	High = 'High'
}

export enum KitchenEquipment {
	Basic = 'Basic',
	Standard = 'Standard',
	Advanced = 'Advanced'
}

export enum MealPrepTime {
	Under15Min = 'Under15Min',
	From15To30Min = 'From15To30Min',
	From30To60Min = 'From30To60Min',
	Over60Min = 'Over60Min'
}

// Request used when creating or updating a user profile
export interface CreateUserProfileRequest {
	age: number;
	height: number; // cm
	weight: number; // kg
	activityLevel: ActivityLevel;
	dietaryPreference: DietaryPreference;
	allergens: Allergen[];
	healthGoals: HealthGoal[];
	medicalConditions: MedicalCondition[];
	medications: string[];
	mealPreferences: MealPreference[];
	cookingSkillLevel: CookingSkillLevel;
	budgetRange: BudgetRange;
	kitchenEquipment: KitchenEquipment[];
	mealPrepTime: MealPrepTime;
}

// Full profile returned by backend
export interface UserProfile extends CreateUserProfileRequest {
	id: string;
	userId?: string; // link to owning user
	createdAt?: string;
	updatedAt?: string;
	// Derived metrics that backend might compute (optional)
	bmi?: number;
	bmr?: number;
	dailyCalorieTarget?: number;
}

