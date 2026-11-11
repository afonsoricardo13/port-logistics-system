import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

/**
 * OAuth Callback Component
 * US 3.2.1 - Handle OAuth2 callback from Google
 * 
 * This component handles the redirect from Google after authentication.
 * It extracts tokens from URL fragment and completes the login flow.
 * 
 * AC: After successful authentication, a valid access token must be available to the front-end
 */
@Component({
  selector: 'app-callback',
  imports: [],
  template: `
    <div style="display: flex; justify-content: center; align-items: center; height: 100vh;">
      <div style="text-align: center;">
        <h2>Authenticating...</h2>
        <p>Please wait while we complete your login.</p>
      </div>
    </div>
  `,
  styles: []
})
export class CallbackComponent implements OnInit {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  async ngOnInit(): Promise<void> {
    // Process OAuth callback - extract tokens from URL fragment
    // US 3.2.2: Also load user role from backend
    const success = await this.authService.handleCallback();
    
    if (success) {
      // US 3.2.6: Check if there's a return URL (for activation flow)
      const returnUrl = sessionStorage.getItem('returnUrl');
      
      if (returnUrl) {
        // User is in activation flow - redirect to activation page
        console.log('🔄 Redirecting to activation page:', returnUrl);
        setTimeout(() => {
          this.router.navigateByUrl(returnUrl);
        }, 500);
      } else {
        // Normal login - redirect to dashboard
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 500);
      }
    } else {
      // Check if user was denied access (already redirected to /access-denied)
      const accessDenied = sessionStorage.getItem('access_denied_reason');
      
      if (accessDenied) {
        // User was denied access due to no role/inactive - already on /access-denied page
        console.error('Access denied:', accessDenied);
        // Don't redirect - let the user stay on access-denied page
      } else {
        // No tokens found - redirect back to login
        console.error('Authentication failed - no tokens received');
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1000);
      }
    }
  }
}
