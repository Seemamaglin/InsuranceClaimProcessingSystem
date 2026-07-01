import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, FormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private fb = inject(FormBuilder);

  registerForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    dob: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    username: ['', Validators.required],
    phone: ['', [Validators.required, Validators.pattern('^[0-9]{10}$')]],
    password: ['', Validators.required],
    confirmPassword: ['', Validators.required]
  });

  showPassword = false;

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  get password() {
    return this.registerForm.get('password')?.value || '';
  }

  get hasLength() {
    return this.password.length >= 8;
  }

  get hasNumberAndSpecial() {
    return /[0-9]/.test(this.password) && /[^a-zA-Z0-9]/.test(this.password);
  }

  get hasUpperAndLower() {
    return /[a-z]/.test(this.password) && /[A-Z]/.test(this.password);
  }

  get noCommonWords() {
    if (!this.password) return false;
    const common = ['password', '12345678', 'admin', 'qwerty'];
    return !common.includes(this.password.toLowerCase());
  }

  get score() {
    if (!this.password) return 0;
    let s = 0;
    if (this.hasLength) s++;
    if (this.hasNumberAndSpecial) s++;
    if (this.hasUpperAndLower) s++;
    if (this.noCommonWords) s++;
    return s;
  }

  get strengthText() {
    if (this.score === 0) return '';
    if (this.score === 1) return 'WEAK PASSWORD';
    if (this.score === 2) return 'FAIR PASSWORD';
    if (this.score === 3) return 'GOOD PASSWORD';
    return 'STRONG PASSWORD';
  }

  currentStep = 1;
  isRegistered = false;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  private authService = inject(AuthService);
  private router = inject(Router);

  onSubmit() {
    if (this.registerForm.invalid) {
      this.errorMessage = 'Please fix the validation errors before submitting.';
      return;
    }
    
    if (this.password !== this.registerForm.get('confirmPassword')?.value) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    
    const formValue = this.registerForm.value;
    const payload = {
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      email: formValue.email,
      userName: formValue.username, // Map 'username' to 'userName'
      phoneNumber: `+91${formValue.phone}`, // Prepend country code and map 'phone' to 'phoneNumber'
      password: formValue.password,
      confirmPassword: formValue.confirmPassword,
      dateOfBirth: new Date(formValue.dob ?? '').toISOString(),
      role: 5 // PolicyHolder is 5 in backend enum
    };

    this.authService.register(payload).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = '';
        this.isRegistered = true;
        this.currentStep = 2; // Move to Verification step
        this.startResendTimer();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Registration failed. Username or email might already exist.';
      }
    });
  }

  nextStep() {
    if (this.currentStep === 1) {
      if (this.isRegistered) {
        this.currentStep = 2;
        this.startResendTimer();
        return;
      }
      this.onSubmit();
    } else if (this.currentStep === 2) {
      if (!this.verificationCode) {
        alert("Please enter the verification code sent to your email.");
        return;
      }
      this.isLoading = true;
      const email = this.registerForm.value.email ?? '';
      this.authService.verifyEmail(email, this.verificationCode).subscribe({
        next: () => {
          // Email verified successfully! 
          // Now we must log the user in to get a valid JWT token for the KYC upload step.
          const password = this.registerForm.value.password || '';
          this.authService.login(email, password).subscribe({
            next: () => {
              this.isLoading = false;
              this.currentStep = 3;
            },
            error: () => {
              this.isLoading = false;
              alert("Email verified, but auto-login failed. Please proceed to login manually.");
              this.currentStep = 4; // or redirect to login
            }
          });
        },
        error: (err) => {
          this.isLoading = false;
          alert(err.error?.message || 'Invalid verification code. Please try again.');
        }
      });
    } else if (this.currentStep === 3) {
      this.submitKyc();
    }
  }

  prevStep() {
    if (this.currentStep > 1 && this.currentStep < 4) {
      this.currentStep--;
    }
  }

  // --- Step 2: Verification Logic ---
  verificationCode = '';
  resendCountdown = 42;
  resendInterval: any;

  startResendTimer() {
    if (this.resendCountdown > 0 && this.resendCountdown < 42) return;
    this.resendCountdown = 42;
    clearInterval(this.resendInterval);
    this.resendInterval = setInterval(() => {
      if (this.resendCountdown > 0) {
        this.resendCountdown--;
      } else {
        clearInterval(this.resendInterval);
      }
    }, 1000);
  }

  resendEmail() {
    this.startResendTimer();
    // By re-submitting the registration payload, the backend will soft-delete the old unverified user
    // and recreate a new one, which sends a fresh code to the email!
    this.onSubmit();
  }

  simulateEmailVerified() {
    // For testing purposes, skip to Step 3 manually
    this.currentStep = 3;
  }

  // --- Step 3: KYC Upload Logic ---
  aadhaarFile: File | null = null;
  panFile: File | null = null;
  readonly MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB

  async onFileSelected(event: any, type: 'aadhaar' | 'pan') {
    let file = event.target.files[0];
    if (!file) return;

    if (file.size > this.MAX_FILE_SIZE) {
      if (file.type.startsWith('image/')) {
        try {
          this.isLoading = true;
          file = await this.compressImageToWebp(file);
          this.isLoading = false;
        } catch (err) {
          this.isLoading = false;
          alert("Failed to compress image. Please upload a smaller file.");
          return;
        }
      } else {
        alert(`File exceeds 5MB limit. Please upload a smaller file.`);
        return;
      }
    }

    if (type === 'aadhaar') this.aadhaarFile = file;
    if (type === 'pan') this.panFile = file;
  }

  private compressImageToWebp(file: File): Promise<File> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (e: any) => {
        const img = new Image();
        img.onload = () => {
          const canvas = document.createElement('canvas');
          let width = img.width;
          let height = img.height;

          // Optional: scale down if extremely large
          const MAX_DIMENSION = 2048;
          if (width > MAX_DIMENSION || height > MAX_DIMENSION) {
            if (width > height) {
              height = Math.round((height * MAX_DIMENSION) / width);
              width = MAX_DIMENSION;
            } else {
              width = Math.round((width * MAX_DIMENSION) / height);
              height = MAX_DIMENSION;
            }
          }

          canvas.width = width;
          canvas.height = height;
          const ctx = canvas.getContext('2d');
          if (!ctx) {
            reject(new Error('Failed to get canvas context'));
            return;
          }
          ctx.drawImage(img, 0, 0, width, height);

          // Convert to WebP with 0.8 quality
          canvas.toBlob((blob) => {
            if (blob) {
              const newName = file.name.substring(0, file.name.lastIndexOf('.')) + '.webp';
              const webpFile = new File([blob], newName, {
                type: 'image/webp',
                lastModified: Date.now(),
              });
              resolve(webpFile);
            } else {
              reject(new Error('Canvas to Blob failed'));
            }
          }, 'image/webp', 0.8);
        };
        img.onerror = () => reject(new Error('Failed to load image'));
        img.src = e.target.result;
      };
      reader.onerror = () => reject(new Error('Failed to read file'));
      reader.readAsDataURL(file);
    });
  }

  submitKyc() {
    if (!this.aadhaarFile && !this.panFile) {
      alert("Please upload at least one document or click 'Skip for now'.");
      return;
    }

    this.isLoading = true;
    
    const formData = new FormData();
    if (this.aadhaarFile) formData.append('documents', this.aadhaarFile);
    if (this.panFile) formData.append('documents', this.panFile);

    this.authService.submitKyc(formData).subscribe({
      next: () => {
        this.isLoading = false;
        this.currentStep = 4;
      },
      error: (err) => {
        this.isLoading = false;
        alert(err.error?.message || 'Failed to upload documents.');
      }
    });
  }

  skipKyc() {
    this.currentStep = 4;
  }
}
