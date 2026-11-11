import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Administrator Role Guard
 * US 3.2.5 - Ensure only Administrators can access user management
 * 
 * This guard ensures that only users with Administrator role can access admin-only routes.
 * - Non-administrators are redirected to access-denied page
 * - Must be used in combination with authGuard (authentication is checked first)
 * 
 * AC: Only Administrator role can access user management functionality
 */
export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Get user role (authGuard should have already verified authentication)
  const userRole = authService.getUserRole();
  
  // Check if user has Administrator role
  if (userRole !== 'Administrator') {
    console.error('❌ Access denied: Administrator role required');
    router.navigate(['/access-denied']);
    return false;
  }

  // User has Administrator role
  return true;
};
