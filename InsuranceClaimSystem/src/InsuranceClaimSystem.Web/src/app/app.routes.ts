import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { PolicyBrowserComponent } from './policy-browser/policy-browser.component';
import { authGuard } from './guards/auth.guard';
import { isFirstLoginGuard } from './guards/is-first-login.guard';
import { ApplyPolicyComponent } from './apply-policy/apply-policy.component';

import { RegisterComponent } from './register/register.component';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { SetPasswordComponent } from './set-password/set-password.component';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { StaffLayoutComponent } from './layouts/staff-layout/staff-layout.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  { 
    path: 'set-password', 
    component: SetPasswordComponent,
    canActivate: [authGuard] // Only authGuard, because we want them to access this even if isFirstLogin is true
  },
  
  // PolicyHolder Routes (Main Layout)
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard, isFirstLoginGuard],
    children: [
      { path: 'browse-policies', component: PolicyBrowserComponent },
      { path: 'apply-policy/:id', component: ApplyPolicyComponent },
      // my-claims will go here
    ]
  },

  // Staff Routes (Staff Layout)
  {
    path: '',
    component: StaffLayoutComponent,
    canActivate: [authGuard, isFirstLoginGuard],
    children: [
      { path: 'admin-dashboard', loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard.component').then(c => c.AdminDashboardComponent) }
      // reviewer-dashboard, finance-dashboard will go here
    ]
  },

  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'browse-policies' }
];
