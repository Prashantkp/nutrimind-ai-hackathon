import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

// Angular Material imports
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSliderModule } from '@angular/material/slider';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatStepperModule } from '@angular/material/stepper';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';

// Services and Models
import { ProfileService } from '../../../core/services/profile.service';
import { AuthService } from '../../../core/services/auth.service';
import { HeaderComponent } from '../../../shared/components/header.component';
import { 
  CreateUserProfileRequest, 
  ActivityLevel, 
  DietaryPreference, 
  Allergen, 
  HealthGoal, 
  MedicalCondition,
  MealPreference,
  CookingSkillLevel,
  BudgetRange,
  KitchenEquipment,
  MealPrepTime
} from '../../../shared/models/profile.models';

@Component({
  selector: 'app-profile-setup',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatSliderModule,
    MatCheckboxModule,
    MatChipsModule,
    MatIconModule,
    MatStepperModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    HeaderComponent
  ],
  templateUrl: './profile-setup.html',
  styleUrl: './profile-setup.scss'
})
export class ProfileSetupComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private profileService = inject(ProfileService);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);

  personalInfoForm: FormGroup;
  healthGoalsForm: FormGroup;
  dietaryPreferencesForm: FormGroup;
  
  isLoading = false;
  
  // Form options mapped to backend enums
  genderOptions = ['Male', 'Female', 'Other', 'Prefer not to say'];
  
  activityLevels = [
    { value: ActivityLevel.Sedentary, label: 'Sedentary (little or no exercise)' },
    { value: ActivityLevel.LightlyActive, label: 'Lightly active (light exercise 1-3 days/week)' },
    { value: ActivityLevel.ModeratelyActive, label: 'Moderately active (moderate exercise 3-5 days/week)' },
    { value: ActivityLevel.VeryActive, label: 'Very active (hard exercise 6-7 days/week)' },
    { value: ActivityLevel.SuperActive, label: 'Super active (very hard exercise & physical job)' }
  ];

  healthGoals = [
    { value: HealthGoal.WeightLoss, label: 'Weight Loss' },
    { value: HealthGoal.WeightGain, label: 'Weight Gain' },
    { value: HealthGoal.MuscleGain, label: 'Muscle Building' },
    { value: HealthGoal.ImprovedEnergy, label: 'Improved Energy' },
    { value: HealthGoal.BetterDigestion, label: 'Better Digestion' },
    { value: HealthGoal.HeartHealth, label: 'Heart Health' },
    { value: HealthGoal.BloodSugarControl, label: 'Blood Sugar Control' },
    { value: HealthGoal.ReduceInflammation, label: 'Reduce Inflammation' }
  ];

  dietaryPreferences = [
    { value: DietaryPreference.None, label: 'None' },
    { value: DietaryPreference.Vegetarian, label: 'Vegetarian' },
    { value: DietaryPreference.Vegan, label: 'Vegan' },
    { value: DietaryPreference.Pescatarian, label: 'Pescatarian' },
    { value: DietaryPreference.Keto, label: 'Keto' },
    { value: DietaryPreference.Paleo, label: 'Paleo' },
    { value: DietaryPreference.Mediterranean, label: 'Mediterranean' },
    { value: DietaryPreference.LowCarb, label: 'Low Carb' },
    { value: DietaryPreference.GlutenFree, label: 'Gluten-Free' }
  ];

  allergens = [
    { value: Allergen.Milk, label: 'Milk/Dairy' },
    { value: Allergen.Eggs, label: 'Eggs' },
    { value: Allergen.Fish, label: 'Fish' },
    { value: Allergen.Shellfish, label: 'Shellfish' },
    { value: Allergen.TreeNuts, label: 'Tree Nuts' },
    { value: Allergen.Peanuts, label: 'Peanuts' },
    { value: Allergen.Wheat, label: 'Wheat/Gluten' },
    { value: Allergen.Soybeans, label: 'Soy' },
    { value: Allergen.Sesame, label: 'Sesame' }
  ];

  medicalConditions = [
    { value: MedicalCondition.Diabetes, label: 'Diabetes' },
    { value: MedicalCondition.Hypertension, label: 'Hypertension' },
    { value: MedicalCondition.HeartDisease, label: 'Heart Disease' },
    { value: MedicalCondition.KidneyDisease, label: 'Kidney Disease' },
    { value: MedicalCondition.Thyroid, label: 'Thyroid Issues' },
    { value: MedicalCondition.PCOS, label: 'PCOS' },
    { value: MedicalCondition.IBD, label: 'IBD/Crohn\'s' }
  ];

  cookingSkillLevels = [
    { value: CookingSkillLevel.Beginner, label: 'Beginner' },
    { value: CookingSkillLevel.Intermediate, label: 'Intermediate' },
    { value: CookingSkillLevel.Advanced, label: 'Advanced' }
  ];

  mealPrepTimes = [
    { value: MealPrepTime.Under15Min, label: 'Under 15 minutes' },
    { value: MealPrepTime.From15To30Min, label: '15-30 minutes' },
    { value: MealPrepTime.From30To60Min, label: '30-60 minutes' },
    { value: MealPrepTime.Over60Min, label: 'Over 60 minutes' }
  ];

  budgetRanges = [
    { value: BudgetRange.Low, label: 'Low ($20-40/week)' },
    { value: BudgetRange.Medium, label: 'Medium ($40-80/week)' },
    { value: BudgetRange.High, label: 'High ($80+/week)' }
  ];

  constructor() {
    this.personalInfoForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      dateOfBirth: ['', Validators.required],
      gender: ['', Validators.required],
      height: ['', [Validators.required, Validators.min(100), Validators.max(250)]],
      weight: ['', [Validators.required, Validators.min(30), Validators.max(300)]],
      activityLevel: ['', Validators.required]
    });

    this.healthGoalsForm = this.fb.group({
      primaryGoals: [[], Validators.required],
      medicalConditions: [[]],
      medications: ['']
    });

    this.dietaryPreferencesForm = this.fb.group({
      dietaryPreference: [DietaryPreference.None, Validators.required],
      allergens: [[]],
      cookingSkillLevel: ['', Validators.required],
      budgetRange: ['', Validators.required],
      mealPrepTime: ['', Validators.required]
    });
  }

  onPersonalInfoSubmit() {
    if (this.personalInfoForm.valid) {
      console.log('Personal Info:', this.personalInfoForm.value);
      // Move to next step or save data
    } else {
      this.markFormGroupTouched(this.personalInfoForm);
    }
  }

  onHealthGoalsSubmit() {
    if (this.healthGoalsForm.valid) {
      console.log('Health Goals:', this.healthGoalsForm.value);
      // Move to next step or save data
    } else {
      this.markFormGroupTouched(this.healthGoalsForm);
    }
  }

  onDietaryPreferencesSubmit() {
    if (this.dietaryPreferencesForm.valid) {
      console.log('Dietary Preferences:', this.dietaryPreferencesForm.value);
      this.completeProfileSetup();
    } else {
      this.markFormGroupTouched(this.dietaryPreferencesForm);
    }
  }

  onGoalChange(goal: HealthGoal, isChecked: boolean) {
    const goalsArray = this.healthGoalsForm.get('primaryGoals')?.value || [];
    if (isChecked) {
      if (!goalsArray.includes(goal)) {
        goalsArray.push(goal);
      }
    } else {
      const index = goalsArray.indexOf(goal);
      if (index > -1) {
        goalsArray.splice(index, 1);
      }
    }
    this.healthGoalsForm.patchValue({ primaryGoals: goalsArray });
  }

  onMedicalConditionChange(condition: MedicalCondition, isChecked: boolean) {
    const conditionsArray = this.healthGoalsForm.get('medicalConditions')?.value || [];
    if (isChecked) {
      if (!conditionsArray.includes(condition)) {
        conditionsArray.push(condition);
      }
    } else {
      const index = conditionsArray.indexOf(condition);
      if (index > -1) {
        conditionsArray.splice(index, 1);
      }
    }
    this.healthGoalsForm.patchValue({ medicalConditions: conditionsArray });
  }

  onAllergenChange(allergen: Allergen, isChecked: boolean) {
    const allergensArray = this.dietaryPreferencesForm.get('allergens')?.value || [];
    if (isChecked) {
      if (!allergensArray.includes(allergen)) {
        allergensArray.push(allergen);
      }
    } else {
      const index = allergensArray.indexOf(allergen);
      if (index > -1) {
        allergensArray.splice(index, 1);
      }
    }
    this.dietaryPreferencesForm.patchValue({ allergens: allergensArray });
  }

  completeProfileSetup() {
    if (!this.personalInfoForm.valid || !this.healthGoalsForm.valid || !this.dietaryPreferencesForm.valid) {
      this.snackBar.open('Please fill in all required fields', 'Close', { duration: 3000 });
      return;
    }

    this.isLoading = true;

    // Calculate age from date of birth
    const birthDate = new Date(this.personalInfoForm.get('dateOfBirth')?.value);
    const today = new Date();
    const age = today.getFullYear() - birthDate.getFullYear();

    // Map form data to backend API format
    const profileData: CreateUserProfileRequest = {
      age: age,
      height: this.personalInfoForm.get('height')?.value,
      weight: this.personalInfoForm.get('weight')?.value,
      activityLevel: this.personalInfoForm.get('activityLevel')?.value,
      dietaryPreference: this.dietaryPreferencesForm.get('dietaryPreference')?.value,
      allergens: this.dietaryPreferencesForm.get('allergens')?.value || [],
      healthGoals: this.healthGoalsForm.get('primaryGoals')?.value || [],
      medicalConditions: this.healthGoalsForm.get('medicalConditions')?.value || [],
      medications: this.healthGoalsForm.get('medications')?.value ? 
                   [this.healthGoalsForm.get('medications')?.value] : [],
      mealPreferences: [], // Can be expanded later
      cookingSkillLevel: this.dietaryPreferencesForm.get('cookingSkillLevel')?.value,
      budgetRange: this.dietaryPreferencesForm.get('budgetRange')?.value,
      kitchenEquipment: [], // Can be expanded later
      mealPrepTime: this.dietaryPreferencesForm.get('mealPrepTime')?.value
    };

    console.log('Creating profile with data:', profileData);

    // Call the backend API to create the profile
    this.profileService.createProfile(profileData).subscribe({
      next: (response) => {
        console.log('Profile created successfully:', response);
        this.isLoading = false;
        
        // Update the user's profile status in the auth service
        this.authService.updateProfileStatus(true);
        
        this.snackBar.open('Profile created successfully!', 'Close', { 
          duration: 3000,
          panelClass: ['success-snackbar']
        });
        
        // Navigate to dashboard after successful profile creation
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 1500);
      },
      error: (error) => {
        console.error('Profile creation failed:', error);
        this.isLoading = false;
        this.snackBar.open(
          error.message || 'Failed to create profile. Please try again.', 
          'Close', 
          { duration: 5000, panelClass: ['error-snackbar'] }
        );
      }
    });
  }

  private markFormGroupTouched(formGroup: FormGroup) {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  // Helper method to get form control errors
  getFieldError(formGroup: FormGroup, fieldName: string): string {
    const control = formGroup.get(fieldName);
    if (control?.errors && control?.touched) {
      if (control.errors['required']) {
        return `${fieldName} is required`;
      }
      if (control.errors['minlength']) {
        return `${fieldName} must be at least ${control.errors['minlength'].requiredLength} characters`;
      }
      if (control.errors['min']) {
        return `${fieldName} must be at least ${control.errors['min'].min}`;
      }
      if (control.errors['max']) {
        return `${fieldName} must not exceed ${control.errors['max'].max}`;
      }
    }
    return '';
  }
}
