import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../../core/services/auth.service';
import { UserService } from '../../../../core/services/user.service';
import { User, UserPreferences, ActivityLevel, SkillLevel, MacroGoals } from '../../../../core/models';

@Component({
  selector: 'app-profile-settings',
  templateUrl: './profile-settings.component.html',
  styleUrls: ['./profile-settings.component.scss']
})
export class ProfileSettingsComponent implements OnInit {
  currentUser: User | null = null;
  profileForm: FormGroup;
  preferencesForm: FormGroup;
  isLoading = false;
  isSaving = false;

  // Dropdown options
  activityLevels = [
    { value: ActivityLevel.Sedentary, label: 'Sedentary', description: 'Little to no exercise' },
    { value: ActivityLevel.LightlyActive, label: 'Lightly Active', description: 'Light exercise 1-3 days/week' },
    { value: ActivityLevel.ModeratelyActive, label: 'Moderately Active', description: 'Moderate exercise 3-5 days/week' },
    { value: ActivityLevel.VeryActive, label: 'Very Active', description: 'Hard exercise 6-7 days/week' },
    { value: ActivityLevel.ExtremelyActive, label: 'Extremely Active', description: 'Very hard exercise, physical job' }
  ];

  skillLevelOptions = [
    { value: SkillLevel.Beginner, label: 'Beginner', description: 'Basic cooking skills, simple recipes' },
    { value: SkillLevel.Intermediate, label: 'Intermediate', description: 'Comfortable with most techniques' },
    { value: SkillLevel.Advanced, label: 'Advanced', description: 'Expert level, complex recipes' }
  ];

  dietaryRestrictions = [
    'vegetarian', 'vegan', 'pescatarian', 'gluten-free', 'dairy-free', 
    'nut-free', 'soy-free', 'keto', 'paleo', 'low-carb', 'low-sodium', 
    'diabetic-friendly', 'heart-healthy'
  ];

  commonAllergies = [
    'peanuts', 'tree nuts', 'milk', 'eggs', 'wheat', 'soy', 
    'fish', 'shellfish', 'sesame', 'mustard', 'sulfites'
  ];

  availableCuisines = [
    'American', 'Italian', 'Mexican', 'Chinese', 'Japanese', 'Thai', 
    'Indian', 'Mediterranean', 'French', 'Greek', 'Korean', 'Vietnamese',
    'Middle Eastern', 'Spanish', 'German', 'British'
  ];

  healthGoalOptions = [
    'weight loss', 'weight gain', 'muscle building', 'maintenance', 
    'improved energy', 'better digestion', 'heart health', 'diabetes management',
    'lower cholesterol', 'reduce inflammation', 'better sleep', 'stress reduction'
  ];

  kitchenEquipmentOptions = [
    'stovetop', 'oven', 'microwave', 'slow cooker', 'instant pot', 
    'air fryer', 'blender', 'food processor', 'grill', 'rice cooker',
    'stand mixer', 'immersion blender', 'pressure cooker', 'toaster oven'
  ];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private userService: UserService,
    private snackBar: MatSnackBar
  ) {
    this.profileForm = this.createProfileForm();
    this.preferencesForm = this.createPreferencesForm();
  }

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        this.loadUserProfile();
        this.loadUserPreferences();
      }
    });
  }

  private createProfileForm(): FormGroup {
    return this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]]
    });
  }

  private createPreferencesForm(): FormGroup {
    return this.fb.group({
      dietaryRestrictions: [[]],
      allergies: [[]],
      preferredCuisines: [[]],
      healthGoals: [[]],
      dislikedIngredients: [''],
      kitchenEquipment: [[]],
      maxCookingTime: [60, [Validators.min(10), Validators.max(300)]],
      skillLevel: [SkillLevel.Intermediate],
      activityLevel: [ActivityLevel.ModeratelyActive],
      macroGoals: this.fb.group({
        caloriesPerDay: [2000, [Validators.required, Validators.min(1000), Validators.max(5000)]],
        proteinGrams: [150, [Validators.required, Validators.min(50), Validators.max(500)]],
        carbsGrams: [200, [Validators.required, Validators.min(50), Validators.max(800)]],
        fatGrams: [65, [Validators.required, Validators.min(20), Validators.max(200)]]
      })
    });
  }

  private loadUserProfile(): void {
    if (!this.currentUser) return;
    
    this.profileForm.patchValue({
      firstName: this.currentUser.firstName,
      lastName: this.currentUser.lastName,
      email: this.currentUser.email
    });
  }

  private loadUserPreferences(): void {
    if (!this.currentUser) return;
    
    this.isLoading = true;
    
    this.userService.getUserPreferences(this.currentUser.id).subscribe({
      next: (preferences) => {
        if (preferences) {
          this.preferencesForm.patchValue({
            dietaryRestrictions: preferences.dietaryRestrictions || [],
            allergies: preferences.allergies || [],
            preferredCuisines: preferences.preferredCuisines || [],
            healthGoals: preferences.healthGoals || [],
            dislikedIngredients: (preferences.dislikedIngredients || []).join(', '),
            kitchenEquipment: preferences.kitchenEquipment || [],
            maxCookingTime: preferences.maxCookingTime || 60,
            skillLevel: preferences.skillLevel || SkillLevel.Intermediate,
            activityLevel: preferences.activityLevel || ActivityLevel.ModeratelyActive,
            macroGoals: {
              caloriesPerDay: preferences.macroGoals?.caloriesPerDay || 2000,
              proteinGrams: preferences.macroGoals?.proteinGrams || 150,
              carbsGrams: preferences.macroGoals?.carbsGrams || 200,
              fatGrams: preferences.macroGoals?.fatGrams || 65
            }
          });
        }
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading user preferences:', error);
        this.isLoading = false;
      }
    });
  }

  onSaveProfile(): void {
    if (this.profileForm.valid && this.currentUser) {
      this.isSaving = true;
      
      const updatedUser = {
        ...this.currentUser,
        ...this.profileForm.value
      };

      this.userService.updateUserProfile(this.currentUser.id, updatedUser).subscribe({
        next: (user) => {
          this.snackBar.open('Profile updated successfully!', 'Close', { duration: 3000 });
          this.isSaving = false;
        },
        error: (error) => {
          console.error('Error updating profile:', error);
          this.snackBar.open('Error updating profile. Please try again.', 'Close', { duration: 3000 });
          this.isSaving = false;
        }
      });
    }
  }

  onSavePreferences(): void {
    if (this.preferencesForm.valid && this.currentUser) {
      this.isSaving = true;
      
      const formValue = this.preferencesForm.value;
      const preferences: Partial<UserPreferences> = {
        userId: this.currentUser.id,
        dietaryRestrictions: formValue.dietaryRestrictions,
        allergies: formValue.allergies,
        preferredCuisines: formValue.preferredCuisines,
        healthGoals: formValue.healthGoals,
        dislikedIngredients: formValue.dislikedIngredients 
          ? formValue.dislikedIngredients.split(',').map((s: string) => s.trim()).filter((s: string) => s)
          : [],
        kitchenEquipment: formValue.kitchenEquipment,
        maxCookingTime: formValue.maxCookingTime,
        skillLevel: formValue.skillLevel,
        activityLevel: formValue.activityLevel,
        macroGoals: formValue.macroGoals
      };

      this.userService.updateUserPreferences(this.currentUser.id, this.currentUser.preferences?.id || '', preferences).subscribe({
        next: () => {
          this.snackBar.open('Preferences updated successfully!', 'Close', { duration: 3000 });
          this.isSaving = false;
        },
        error: (error) => {
          console.error('Error updating preferences:', error);
          this.snackBar.open('Error updating preferences. Please try again.', 'Close', { duration: 3000 });
          this.isSaving = false;
        }
      });
    }
  }

  calculateMacroPercentages(): { protein: number; carbs: number; fat: number } {
    const macros = this.preferencesForm.get('macroGoals')?.value;
    if (!macros) return { protein: 0, carbs: 0, fat: 0 };

    const totalCalories = macros.caloriesPerDay;
    const proteinCal = macros.proteinGrams * 4;
    const carbsCal = macros.carbsGrams * 4;
    const fatCal = macros.fatGrams * 9;

    return {
      protein: Math.round((proteinCal / totalCalories) * 100),
      carbs: Math.round((carbsCal / totalCalories) * 100),
      fat: Math.round((fatCal / totalCalories) * 100)
    };
  }

  resetToDefaults(): void {
    this.preferencesForm.reset({
      dietaryRestrictions: [],
      allergies: [],
      preferredCuisines: [],
      healthGoals: [],
      dislikedIngredients: '',
      kitchenEquipment: [],
      maxCookingTime: 60,
      skillLevel: SkillLevel.Intermediate,
      activityLevel: ActivityLevel.ModeratelyActive,
      macroGoals: {
        caloriesPerDay: 2000,
        proteinGrams: 150,
        carbsGrams: 200,
        fatGrams: 65
      }
    });
  }

  deleteAccount(): void {
    // Will implement account deletion with confirmation dialog
    console.log('Delete account requested');
  }

  savePreferences(): void {
    this.onSavePreferences();
  }

  loadProfile(): void {
    console.log('Loading profile data');
    // Will implement profile loading functionality
  }
}
