import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  const expectedRoles = route.data['roles'] as Array<number>; 
  const currentUser = authService.currentUserValue;

  if (currentUser && expectedRoles.includes(currentUser.role)) {
    return true; 
  }

  router.navigate(['/login']);
  return false;
};
