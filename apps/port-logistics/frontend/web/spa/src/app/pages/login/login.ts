import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

/**
 * Login Component
 * US 3.2.1 - Authentication using external IAM provider (Google)
 * US 4.5.2 - Privacy Policy accessibility for non-registered users
 * 
 * This component provides the login interface where users authenticate
 * through Google OAuth2. No password is stored in the system.
 * 
 * AC: The SPA must integrate with the selected IAM (e.g., via OAuth2/OpenID Connect)
 * AC: Unauthenticated users must be redirected to the IAM login page
 * AC: The system must not handle password storage
 * AC: Privacy Policy must be accessible to non-registered users (US 4.5.2)
 */
@Component({
  selector: 'app-login',
  imports: [CommonModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    // Clear any previous error states when arriving at login page
    // This prevents "Access Denied" messages from previous failed attempts
    sessionStorage.removeItem('access_denied_reason');
    sessionStorage.removeItem('access_denied_email');
    
    // Check if user is already authenticated
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
  }

  /**
   * Initiate Google OAuth2 login flow
   * AC: Unauthenticated users must be redirected to the IAM login page
   */
  loginWithGoogle(): void {
    this.authService.login();
  }
}
