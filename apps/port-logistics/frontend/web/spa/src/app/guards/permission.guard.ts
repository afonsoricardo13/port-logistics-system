import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Permission Guard
 * US 3.2.4: Restrict frontend route access based on user permissions
 * 
 * Usage in routes:
 * {
 *   path: 'users',
 *   component: UserManagementComponent,
 *   canActivate: [permissionGuard],
 *   data: { requiredPermission: 'users' }
 * }
 */
export const permissionGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Check if user is authenticated
  if (!authService.isAuthenticated()) {
    console.warn('🔒 Permission Guard: User not authenticated, redirecting to login');
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  // Get required permission from route data
  const requiredPermission = route.data['requiredPermission'] as string | undefined;
  const requiredPermissions = route.data['requiredPermissions'] as string[] | undefined;

  // If no permission required, allow access
  if (!requiredPermission && !requiredPermissions) {
    return true;
  }

  // Check single permission
  if (requiredPermission && !authService.hasPermission(requiredPermission)) {
    console.warn(`🔒 Permission Guard: Access denied - missing permission "${requiredPermission}"`);
    router.navigate(['/no-access'], { 
      queryParams: { 
        requiredPermission,
        attemptedUrl: state.url 
      } 
    });
    return false;
  }

  // Check multiple permissions (user needs at least one)
  if (requiredPermissions && !authService.hasAnyPermission(requiredPermissions)) {
    console.warn(`🔒 Permission Guard: Access denied - missing permissions "${requiredPermissions.join(', ')}"`);
    router.navigate(['/no-access'], { 
      queryParams: { 
        requiredPermissions: requiredPermissions.join(','),
        attemptedUrl: state.url 
      } 
    });
    return false;
  }

  // Permission granted
  console.log(`✅ Permission Guard: Access granted for ${state.url}`);
  return true;
};
