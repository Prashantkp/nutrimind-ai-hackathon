import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {
  registerForm: FormGroup;
  isLoading = false;
  hidePassword = true;
  hideConfirmPassword = true;
  returnUrl = '/dashboard';

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {
    this.registerForm = this.formBuilder.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
      acceptTerms: [false, [Validators.requiredTrue]]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit(): void {
    // Get return url from route parameters or default to dashboard
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
    
    // If user is already logged in, redirect to dashboard
    if (this.authService.isAuthenticated()) {
      this.router.navigate([this.returnUrl]);
    }
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');
    
    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
    } else if (confirmPassword?.hasError('passwordMismatch')) {
      confirmPassword.setErrors(null);
    }
    
    return null;
  }

  onSubmit(): void {
    if (this.registerForm.valid && !this.isLoading) {
      this.isLoading = true;
      
      const userData = {
        firstName: this.registerForm.get('firstName')?.value,
        lastName: this.registerForm.get('lastName')?.value,
        email: this.registerForm.get('email')?.value,
        password: this.registerForm.get('password')?.value
      };

      this.authService.register(userData).subscribe({
        next: (response) => {
          this.snackBar.open('Welcome to NutriMind! Your account has been created successfully! 🎉', 'Close', {
            duration: 4000,
            horizontalPosition: 'right',
            verticalPosition: 'top',
            panelClass: ['success-snackbar']
          });
          this.router.navigate([this.returnUrl]);
        },
        error: (error) => {
          this.isLoading = false;
          const errorMessage = error.error?.message || 'Registration failed. Please try again.';
          this.snackBar.open(errorMessage, 'Close', {
            duration: 5000,
            horizontalPosition: 'right',
            verticalPosition: 'top',
            panelClass: ['error-snackbar']
          });
        }
      });
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.registerForm.controls).forEach(key => {
        this.registerForm.get(key)?.markAsTouched();
      });
    }
  }

  getFirstNameErrorMessage(): string {
    const control = this.registerForm.get('firstName');
    if (control?.hasError('required')) {
      return 'First name is required';
    }
    if (control?.hasError('minlength')) {
      return 'First name must be at least 2 characters';
    }
    return '';
  }

  getLastNameErrorMessage(): string {
    const control = this.registerForm.get('lastName');
    if (control?.hasError('required')) {
      return 'Last name is required';
    }
    if (control?.hasError('minlength')) {
      return 'Last name must be at least 2 characters';
    }
    return '';
  }

  getEmailErrorMessage(): string {
    const emailControl = this.registerForm.get('email');
    if (emailControl?.hasError('required')) {
      return 'Email is required';
    }
    if (emailControl?.hasError('email')) {
      return 'Please enter a valid email address';
    }
    return '';
  }

  getPasswordErrorMessage(): string {
    const passwordControl = this.registerForm.get('password');
    if (passwordControl?.hasError('required')) {
      return 'Password is required';
    }
    if (passwordControl?.hasError('minlength')) {
      return 'Password must be at least 6 characters long';
    }
    return '';
  }

  getConfirmPasswordErrorMessage(): string {
    const control = this.registerForm.get('confirmPassword');
    if (control?.hasError('required')) {
      return 'Please confirm your password';
    }
    if (control?.hasError('passwordMismatch')) {
      return 'Passwords do not match';
    }
    return '';
  }

  navigateToLogin(): void {
    this.router.navigate(['/auth/login'], { 
      queryParams: { returnUrl: this.returnUrl } 
    });
  }
}
