import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { UserManagementService } from '../../services/user-management.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-activate',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activate.html',
  styleUrls: ['./activate.scss']
})
export class ActivateComponent implements OnInit {
  loading = true;
  success = false;
  error: string | null = null;
  token: string | null = null;
  redirectingToLogin = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userManagementService: UserManagementService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Get token from URL query parameter
    this.token = this.route.snapshot.queryParamMap.get('token');

    if (!this.token) {
      this.loading = false;
      this.error = 'Invalid activation link. No token provided.';
      return;
    }

    // First, validate the token status before requiring authentication
    // This prevents unnecessary login attempts for already-used or expired tokens
    this.validateTokenBeforeAuth();
  }

  validateTokenBeforeAuth(): void {
    this.userManagementService.validateActivationToken(this.token!).subscribe({
      next: (validation: any) => {
        if (!validation.isValid) {
          // Token is invalid, already used, or expired - show error immediately
          // Clear any previous auth error state
          sessionStorage.removeItem('access_denied_reason');
          sessionStorage.removeItem('access_denied_email');
          
          this.loading = false;
          this.error = validation.message;
          return;
        }

        // Token is valid - proceed with authentication check
        this.checkAuthenticationAndActivate();
      },
      error: (err: any) => {
        console.error('Token validation error:', err);
        // If validation endpoint fails, proceed with normal flow (backward compatibility)
        this.checkAuthenticationAndActivate();
      }
    });
  }

  checkAuthenticationAndActivate(): void {
    // Check if user is authenticated
    if (!this.authService.isAuthenticated()) {
      // User not authenticated - redirect to login with return URL
      this.redirectingToLogin = true;
      this.loading = false;
      
      // Store token and return URL in sessionStorage to retrieve after login
      sessionStorage.setItem('pendingActivationToken', this.token!);
      sessionStorage.setItem('returnUrl', `/activate?token=${this.token}`);
      
      // Redirect to login
      setTimeout(() => {
        this.router.navigate(['/login'], { 
          queryParams: { returnUrl: `/activate?token=${this.token}` } 
        });
      }, 2000);
      
      return;
    }

    // User is authenticated - proceed with activation
    this.activateAccount();
  }

  activateAccount(): void {
    const userProfile = this.authService.getUserProfile();
    const userEmail = userProfile?.email;

    if (!userEmail || !this.token) {
      this.loading = false;
      this.error = 'Unable to retrieve user information. Please login again.';
      return;
    }

    // Call activation API
    this.userManagementService.activateUser(userEmail, this.token).subscribe({
      next: async () => {
        this.loading = false;
        this.success = true;
        
        // Clear stored token
        sessionStorage.removeItem('pendingActivationToken');
        sessionStorage.removeItem('returnUrl');
        
        // US 3.2.6: After activation, load user role from backend
        console.log('✅ Account activated - loading user role...');
        await this.authService.reloadUserRole();
        
        // Redirect to dashboard after 3 seconds
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 3000);
      },
      error: (err: any) => {
        this.loading = false;
        console.error('Activation error:', err);
        
        // Clear any auth error state that might interfere with subsequent login
        sessionStorage.removeItem('access_denied_reason');
        sessionStorage.removeItem('access_denied_email');
        
        if (err.status === 400) {
          // US 3.2.6 AC: Invalid/expired token OR authenticated user doesn't match link identity
          this.error = 'Invalid or expired activation token, or the link does not belong to your account. Please ensure you are logged in with the correct email address or request a new activation email from your administrator.';
        } else if (err.status === 404) {
          this.error = 'User not found. Please contact your administrator.';
        } else {
          this.error = 'An error occurred during activation. Please try again later or contact your administrator.';
        }
      }
    });
  }

  goToLogin(): void {
    // Clear all session state including auth tokens
    // This ensures a clean slate when redirecting to login after activation failure
    sessionStorage.removeItem('access_denied_reason');
    sessionStorage.removeItem('access_denied_email');
    sessionStorage.removeItem('pendingActivationToken');
    sessionStorage.removeItem('returnUrl');
    
    // If user is authenticated but activation failed, logout first
    if (this.authService.isAuthenticated()) {
      this.authService.logout();
    } else {
      this.router.navigate(['/login']);
    }
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }
}
