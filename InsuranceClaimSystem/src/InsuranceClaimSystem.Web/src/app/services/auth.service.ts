import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, User } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private currentUserSubject = new BehaviorSubject<User | null>(null);

  constructor(private http: HttpClient) {
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
      this.currentUserSubject.next(JSON.parse(savedUser));
    }
  }

  public get currentUserValue(): User | null { return this.currentUserSubject.value; }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<{data: AuthResponse}>(`${this.apiUrl}/login`, { 
        emailOrUsername: email, 
        password: password 
    })
      .pipe(
        map(res => res.data), // Extract from wrapper
        tap(response => {
          this.storeTokens(response);
        })
      );
  }

  register(payload: any): Observable<AuthResponse> {
    return this.http.post<{data: AuthResponse}>(`${this.apiUrl}/register`, payload)
      .pipe(
        map(res => res.data),
        tap(response => {
          this.storeTokens(response);
        })
      );
  }

  verifyEmail(email: string, code: string): Observable<boolean> {
    return this.http.post<{data: boolean}>(`${this.apiUrl}/verify-email`, { email, verificationCode: code })
      .pipe(map(res => res.data));
  }

  submitKyc(formData: FormData): Observable<any> {
    // Note: The backend expects POST /api/Account/kyc, so we adjust the URL
    return this.http.post<{data: any}>(`${environment.apiUrl}/account/kyc`, formData)
      .pipe(map(res => res.data));
  }

  forgotPassword(email: string): Observable<boolean> {
    return this.http.post<{data: boolean}>(`${this.apiUrl}/forgot-password`, { email })
      .pipe(map(res => res.data));
  }

  resetPassword(email: string, resetToken: string, newPassword: string): Observable<boolean> {
    // Note: The backend ResetPasswordAsync expects the reset token to be passed in the `oldPassword` field.
    // It will fall back to validating it as a token if BCrypt.Verify fails.
    return this.http.post<{data: boolean}>(`${this.apiUrl}/reset-password`, { 
      email, 
      oldPassword: resetToken, 
      newPassword 
    })
      .pipe(map(res => res.data));
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem('refreshToken');
    return this.http.post<{data: AuthResponse}>(`${this.apiUrl}/refresh`, { refreshToken })
      .pipe(
        map(res => res.data), // Extract from wrapper
        tap(response => {
          this.storeTokens(response);
        })
      );
  }

  changePassword(currentPassword: string, newPassword: string): Observable<{ success: boolean }> {
    return this.http.post<{ success: boolean }>(`${environment.apiUrl}/account/change-password`, {
      currentPassword,
      newPassword
    });
  }

  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
  }

  private storeTokens(response: AuthResponse): void {
    localStorage.setItem('accessToken', response.token);
    localStorage.setItem('refreshToken', response.refreshToken);
    const user: User = { id: response.userId, fullName: `${response.firstName} ${response.lastName}`, email: response.email, role: response.role, isActive: true };
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSubject.next(user);
  }
}