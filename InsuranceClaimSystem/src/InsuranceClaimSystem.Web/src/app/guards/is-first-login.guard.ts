import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const isFirstLoginGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const currentUser = authService.currentUserValue;

  // If user is logged in but it's their first login, force them to set password
  if (currentUser && currentUser.isFirstLogin && !state.url.includes('/set-password')) {
    router.navigate(['/set-password']);
    return false;
  }
  
  // If user is already on set-password but doesn't need to be there
  if (state.url.includes('/set-password') && currentUser && !currentUser.isFirstLogin) {
    router.navigate(['/']); // send them to dashboard
    return false;
  }

  return true;
};
