import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Authentication and Authorization Guard
 * US 3.2.1 - Protect routes requiring authentication
 * US 3.2.2 - Ensure users have assigned roles and are active
 * 
 * This guard ensures that only authenticated AND authorized users can access protected routes.
 * - Unauthenticated users are redirected to the login page
 * - Authenticated users without role or inactive are redirected to access-denied page
 * 
 * AC: Unauthenticated users must be redirected to the IAM login page
 * AC: If the user has no assigned role or it is inactive, access must be denied
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Check if user is authenticated (has valid Google tokens)
  if (!authService.isAuthenticated()) {
    // Redirect to login page if not authenticated
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  // Check if user has a valid role (authorization)
  const userRole = authService.getUserRole();
  if (!userRole) {
    // User is authenticated but has no role - redirect to access-denied
    console.error('❌ Access denied: User has no role assigned');
    router.navigate(['/access-denied']);
    return false;
  }

  // User is authenticated and authorized
  return true;
};
