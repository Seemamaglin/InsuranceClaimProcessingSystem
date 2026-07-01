import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private authService = inject(AuthService); // <-- 1. Inject the real service!

  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  isLoading = false;
  errorMessage = '';
  showPassword = false;

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  onSubmit() {
    if (this.loginForm.invalid) {
      this.errorMessage = 'Please fill out all fields correctly.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    // The exclamation marks tell TypeScript we are sure these aren't null
    const email = this.loginForm.value.email!;
    const password = this.loginForm.value.password!;

    // 2. Call the real .NET API!
    this.authService.login(email, password).subscribe({
      next: (response: any) => {
        this.isLoading = false;
        
        if (response.isFirstLogin) {
          this.router.navigate(['/set-password']);
          return;
        }

        // 3. Redirect perfectly based on the backend role enum
        switch (response.role) {
          case UserRole.Admin:
            this.router.navigate(['/admin-dashboard']);
            break;
          case UserRole.FinanceOfficer:
            this.router.navigate(['/finance-dashboard']);
            break;
          case UserRole.ClaimsManager:
            this.router.navigate(['/reviewer-dashboard']);
            break;
          case UserRole.PolicyHolder:
          default:
            this.router.navigate(['/browse-policies']);
            break;
        }
      },
      error: (err) => {
        this.isLoading = false;
        // 4. Catch standard backend validation/auth errors
        if (err.status === 401 || err.status === 400) {
          this.errorMessage = 'Invalid email or password.';
        } else {
          this.errorMessage = 'Server error. Is the .NET API running?';
        }
      }
    });
  }
}