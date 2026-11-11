import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Skip interceptor for non-API requests (e.g., Google OAuth callbacks)
  if (!req.url.includes('/api/')) {
    return next(req);
  }

  // Get user profile and token
  const userProfile = authService.getUserProfile();
  const token = authService.getIdToken();

  // If user is authenticated, validate token and add headers
  if (userProfile && token) {
    // Clone request and add authentication headers
    const clonedRequest = req.clone({
      setHeaders: {
        'Authorization': `Bearer ${token}`,          // JWT token for backend validation
        'X-User-Email': userProfile.email            // Email for DevAuthenticationMiddleware
      }
    });

    // Forward the request and handle 401 errors
    return next(clonedRequest).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          // Token was rejected by backend (expired, invalid signature, etc.)
          console.error('AuthInterceptor: Received 401 Unauthorized, token rejected by backend');
          authService.logout(); // Clear invalid session
          router.navigate(['/login']); // Redirect to login
        }
        return throwError(() => error);
      })
    );
  }

  // User not authenticated - let request proceed (will get 401 from backend if protected)
  return next(req);
};
