import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  email = '';
  token = '';

  showNewPassword = false;
  showConfirmPassword = false;

  resetForm = this.fb.group({
    newPassword: ['', [Validators.required, this.passwordStrengthValidator]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  isLoading = false;
  errorMessage = '';
  isTokenExpired = false;
  successMessage = '';

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.token = params['token'] || '';
      this.email = params['email'] || '';

      if (!this.token || !this.email) {
        this.errorMessage = 'Invalid password reset link. Please request a new one.';
        this.isTokenExpired = true;
      }
    });
  }

  toggleNewPassword() {
    this.showNewPassword = !this.showNewPassword;
  }

  toggleConfirmPassword() {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value || '';
    const hasLength = value.length >= 8;
    const hasUpper = /[A-Z]/.test(value);
    const hasNumber = /[0-9]/.test(value);
    const hasSpecial = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]+/.test(value);

    const valid = hasLength && hasUpper && hasNumber && hasSpecial;
    if (!valid) {
      return { passwordStrength: true };
    }
    return null;
  }

  passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const pass = group.get('newPassword')?.value;
    const confirm = group.get('confirmPassword')?.value;
    return pass === confirm ? null : { mismatch: true };
  }

  get hasLength() { return (this.resetForm.get('newPassword')?.value || '').length >= 8; }
  get hasUpper() { return /[A-Z]/.test(this.resetForm.get('newPassword')?.value || ''); }
  get hasNumber() { return /[0-9]/.test(this.resetForm.get('newPassword')?.value || ''); }
  get hasSpecial() { return /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]+/.test(this.resetForm.get('newPassword')?.value || ''); }

  onSubmit() {
    if (this.resetForm.invalid || this.isTokenExpired) return;

    this.isLoading = true;
    this.errorMessage = '';

    const newPassword = this.resetForm.value.newPassword!;

    this.authService.resetPassword(this.email, this.token, newPassword).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Your password has been successfully reset. You can now login.';
      },
      error: (err) => {
        this.isLoading = false;
        // The backend returns 401 Unauthorized with "InvalidToken" code if expired or wrong
        if (err.error?.code === 'InvalidToken') {
          this.errorMessage = 'This reset link has expired or is invalid. Please request a new one.';
          this.isTokenExpired = true;
        } else {
          this.errorMessage = err.error?.message || 'An error occurred while resetting your password.';
        }
      }
    });
  }
}
