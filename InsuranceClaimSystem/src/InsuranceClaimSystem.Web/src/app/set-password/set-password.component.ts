import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-set-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './set-password.component.html',
  styleUrl: './set-password.component.css'
})
export class SetPasswordComponent {
  setPasswordForm: FormGroup;
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.setPasswordForm = this.fb.group({
      oldPassword: ['', Validators.required],
      newPassword: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/)
        ]
      ],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  get hasUpper(): boolean {
    const val = this.setPasswordForm.get('newPassword')?.value || '';
    return /[A-Z]/.test(val);
  }

  get hasNumber(): boolean {
    const val = this.setPasswordForm.get('newPassword')?.value || '';
    return /\d/.test(val);
  }

  get hasSpecial(): boolean {
    const val = this.setPasswordForm.get('newPassword')?.value || '';
    return /[@$!%*?&]/.test(val);
  }

  passwordMatchValidator(g: FormGroup) {
    return g.get('newPassword')?.value === g.get('confirmPassword')?.value
      ? null : { mismatch: true };
  }

  onSubmit() {
    if (this.setPasswordForm.invalid) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    const { oldPassword, newPassword } = this.setPasswordForm.value;

    this.authService.changePassword(oldPassword, newPassword).subscribe({
      next: () => {
        // Clear first login flag in local state
        const user = this.authService.currentUserValue;
        if (user) {
          user.isFirstLogin = false;
          // Trigger the behavior subject to update
          (this.authService as any).currentUserSubject.next(user);
          localStorage.setItem('user', JSON.stringify(user));
        }
        this.router.navigate(['/']); // Redirect to dashboard
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.error?.message || 'Failed to update password. Please check your old password.';
      }
    });
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
