import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PermissionsService } from './permissions.service';

/**
 * Authentication Service
 * US 3.2.1 - External IAM Authentication using Google OAuth2
 * US 3.2.4 - Frontend Authorization based on User Roles
 * 
 * Manual OAuth2 Implicit Flow implementation (no library dependency issues)
 * 
 * Responsibilities:
 * - Manage OAuth2 Implicit Flow with Google manually
 * - Handle login/logout flows
 * - Manage ID tokens securely in sessionStorage
 * - Provide authentication state to components
 * - Manage role-based permissions (US 3.2.4)
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$: Observable<boolean> = this.isAuthenticatedSubject.asObservable();

  private readonly GOOGLE_AUTH_URL = 'https://accounts.google.com/o/oauth2/v2/auth';
  private readonly TOKEN_KEY = 'google_id_token';
  private readonly TOKEN_EXPIRY_KEY = 'google_token_expiry';
  private readonly USER_KEY = 'google_user_profile';
  private readonly ROLE_KEY = 'user_role';

  // US 3.2.3: Token expiry monitoring for silent refresh
  private readonly CHECK_INTERVAL_MS = 60000; // Check every minute
  private readonly REFRESH_THRESHOLD_MS = 300000; // Refresh 5 minutes before expiry
  private tokenExpiryCheckInterval: any;

  constructor(
    private router: Router,
    private permissionsService: PermissionsService
  ) {
    // Check if already authenticated on service init
    this.checkAuthentication();
    
    // US 3.2.3: Start token expiry monitoring
    this.startTokenExpiryMonitoring();
  }

  /**
   * Check authentication status from stored token
   */
  private checkAuthentication(): void {
    const token = sessionStorage.getItem(this.TOKEN_KEY);
    if (token && this.isTokenValid(token)) {
      this.isAuthenticatedSubject.next(true);
    }
  }

  /**
   * Initiate login flow with Google
   * AC: Unauthenticated users must be redirected to the IAM login page
   * AC: The system must not handle password storage
   */
  login(): void {
    const params = new URLSearchParams({
      client_id: environment.oauth.clientId,
      redirect_uri: environment.oauth.redirectUri,
      response_type: 'id_token token',
      scope: environment.oauth.scope,
      nonce: this.generateNonce(),
      state: this.generateState(),
      prompt: 'select_account' // Force account picker to allow testing different users
    });

    // Redirect to Google OAuth
    window.location.href = `${this.GOOGLE_AUTH_URL}?${params.toString()}`;
  }

  /**
   * Handle OAuth callback and extract tokens from URL fragment
   * AC: After successful authentication, a valid access token must be available
   * US 3.2.2: After IAM login, call backend endpoint to retrieve user's assigned role
   */
  async handleCallback(): Promise<boolean> {
    const hash = window.location.hash.substring(1);
    const params = new URLSearchParams(hash);

    const idToken = params.get('id_token');
    const accessToken = params.get('access_token');

    if (idToken) {
      // Store tokens securely (AC - Access tokens must be securely stored)
      sessionStorage.setItem(this.TOKEN_KEY, idToken);
      if (accessToken) {
        sessionStorage.setItem('google_access_token', accessToken);
      }

      // Decode and store user profile
      const userProfile = this.decodeIdToken(idToken);
      if (userProfile) {
        sessionStorage.setItem(this.USER_KEY, JSON.stringify(userProfile));
        
        // US 3.2.6: Check if there's a pending activation token
        // If user is completing activation flow, skip role loading (will be done after activation)
        const pendingActivationToken = sessionStorage.getItem('pendingActivationToken');
        const returnUrl = sessionStorage.getItem('returnUrl');
        
        if (pendingActivationToken && returnUrl?.includes('/activate')) {
          // User is in activation flow - skip role check, let activation page handle it
          console.log('🔄 Activation flow detected - skipping role check');
          this.isAuthenticatedSubject.next(true);
          return true;
        }
        
        // US 3.2.2: Load user role from backend
        const roleLoaded = await this.loadUserRole(userProfile.email);
        
        if (roleLoaded) {
          this.isAuthenticatedSubject.next(true);
        } else {
          // User has no role or is inactive - already redirected to access-denied
          // Clear tokens but keep access denied reason for display
          const reason = sessionStorage.getItem('access_denied_reason');
          const userEmail = userProfile.email;
          sessionStorage.clear();
          sessionStorage.setItem('access_denied_reason', reason || 'Access denied');
          sessionStorage.setItem('access_denied_email', userEmail);
          return false;
        }
      }

      // Clean URL
      window.history.replaceState({}, document.title, window.location.pathname);
      
      return true;
    }

    return false;
  }

  /**
   * Reload user role after activation
   * US 3.2.6: After account activation, reload role from backend
   */
  async reloadUserRole(): Promise<void> {
    const userProfile = this.getUserProfile();
    if (userProfile?.email) {
      await this.loadUserRole(userProfile.email);
    }
  }

  /**
   * Load user authorization role from backend
   * US 3.2.2 AC: After IAM login, the SPA must call a backend endpoint to retrieve the user's assigned role
   * US 3.2.2 AC: If the user has no assigned role or it is inactive, access must be denied
   * US 3.2.4: Load role and permissions for frontend authorization
   */
  private async loadUserRole(email: string): Promise<boolean> {
    try {
      const response = await fetch(
        `${environment.apiUrl}/api/authorization/role?email=${encodeURIComponent(email)}`
      );

      if (response.ok) {
        const data = await response.json();
        
        if (data.active && data.role) {
          // Store role information
          sessionStorage.setItem(this.ROLE_KEY, data.role);
          const permissions = this.permissionsService.getPermissionsForRole(data.role);
          console.log('✅ User role loaded:', data.role, 'with', permissions.length, 'permissions');
          return true;
        } else {
          // User inactive or no role - redirect to access-denied
          console.error('❌ Access denied: User inactive or no role');
          sessionStorage.setItem('access_denied_reason', 'No role assigned or account inactive');
          this.router.navigate(['/access-denied']);
          return false;
        }
      } else if (response.status === 403 || response.status === 404) {
        // Access denied - redirect to access-denied page
        const data = await response.json().catch(() => ({ message: 'Access denied' }));
        console.error('❌ Access denied:', data.message);
        sessionStorage.setItem('access_denied_reason', data.message || 'User not authorized');
        this.router.navigate(['/access-denied']);
        return false;
      } else {
        console.error('❌ Error loading user role:', response.status);
        sessionStorage.setItem('access_denied_reason', 'Error loading user authorization');
        this.router.navigate(['/access-denied']);
        return false;
      }
    } catch (error) {
      console.error('❌ Error calling authorization endpoint:', error);
      sessionStorage.setItem('access_denied_reason', 'Error connecting to authorization service');
      this.router.navigate(['/access-denied']);
      return false;
    }
  }

  /**
   * Handle logout
   * AC: Logout must be supported, clearing tokens/session data
   * US 3.2.2: Also clear role information
   * US 3.2.4: Clear permissions
   */
  logout(): void {
    // US 3.2.3: Stop token monitoring on logout
    this.stopTokenExpiryMonitoring();
    
    sessionStorage.removeItem(this.TOKEN_KEY);
    sessionStorage.removeItem(this.TOKEN_EXPIRY_KEY);
    sessionStorage.removeItem('google_access_token');
    sessionStorage.removeItem(this.USER_KEY);
    sessionStorage.removeItem(this.ROLE_KEY);
    sessionStorage.removeItem('user_active');
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/login']);
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    const token = sessionStorage.getItem(this.TOKEN_KEY);
    return token !== null && this.isTokenValid(token);
  }

  /**
   * Get ID token
   */
  getIdToken(): string | null {
    return sessionStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Get access token
   */
  getAccessToken(): string | null {
    return sessionStorage.getItem('google_access_token');
  }

  /**
   * Get user profile information from stored data
   * Returns user data provided by Google (email, name, picture)
   */
  getUserProfile(): any {
    const profileJson = sessionStorage.getItem(this.USER_KEY);
    return profileJson ? JSON.parse(profileJson) : null;
  }

  /**
   * Get user's email address
   * US 4.1.2: Returns the user's email for API authentication
   */
  getUserEmail(): string | null {
    const profile = this.getUserProfile();
    return profile?.email || null;
  }

  /**
   * Get user's internal authorization role
   * US 3.2.2: Returns the role assigned by the backend after authentication
   */
  getUserRole(): string | null {
    return sessionStorage.getItem(this.ROLE_KEY);
  }

  /**
   * Get user's permissions based on their role
   * US 3.2.4: Returns permissions array for frontend authorization
   */
  getUserPermissions(): string[] {
    const role = this.getUserRole();
    if (!role) {
      return [];
    }
    return this.permissionsService.getPermissionsForRole(role);
  }

  /**
   * Check if user has a specific permission
   * US 3.2.4: Used by guards and components to check access
   */
  hasPermission(permission: string): boolean {
    const role = this.getUserRole();
    if (!role) {
      return false;
    }
    return this.permissionsService.roleHasPermission(role, permission);
  }

  /**
   * Check if user has any of the specified permissions
   * US 3.2.4: Used when multiple permissions grant access
   */
  hasAnyPermission(requiredPermissions: string[]): boolean {
    const role = this.getUserRole();
    if (!role) {
      return false;
    }
    return this.permissionsService.roleHasAnyPermission(role, requiredPermissions);
  }

  /**
   * Check if user account is active
   * US 3.2.2: Returns active status from backend
   */
  isUserActive(): boolean {
    const active = sessionStorage.getItem('user_active');
    return active === 'true';
  }

  /**
   * Decode JWT ID token to extract user information
   */
  private decodeIdToken(token: string): any {
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error('Error decoding ID token:', error);
      return null;
    }
  }

  /**
   * Check if token is still valid (basic expiration check)
   */
  private isTokenValid(token: string): boolean {
    const decoded = this.decodeIdToken(token);
    if (!decoded || !decoded.exp) return false;

    const expirationTime = decoded.exp * 1000; // Convert to milliseconds
    return Date.now() < expirationTime;
  }

  /**
   * Generate random nonce for security
   */
  private generateNonce(): string {
    return Math.random().toString(36).substring(2, 15) +
           Math.random().toString(36).substring(2, 15);
  }

  /**
   * Generate random state for CSRF protection
   */
  private generateState(): string {
    return Math.random().toString(36).substring(2, 15) +
           Math.random().toString(36).substring(2, 15);
  }

  /**
   * US 3.2.3: Start monitoring token expiration
   * Checks every minute if token is about to expire and attempts silent refresh
   */
  private startTokenExpiryMonitoring(): void {
    // Clear any existing interval
    this.stopTokenExpiryMonitoring();

    this.tokenExpiryCheckInterval = setInterval(() => {
      const token = sessionStorage.getItem(this.TOKEN_KEY);
      if (!token) {
        return; // Not authenticated, nothing to monitor
      }

      const decoded = this.decodeIdToken(token);
      if (!decoded || !decoded.exp) {
        return;
      }

      const expirationTime = decoded.exp * 1000; // Convert to milliseconds
      const currentTime = Date.now();
      const timeUntilExpiry = expirationTime - currentTime;

      // If token expired, force logout
      if (timeUntilExpiry <= 0) {
        console.warn('AuthService: Token has expired, logging out');
        this.handleExpiredToken();
        return;
      }

      // If token expires within threshold, attempt silent refresh
      if (timeUntilExpiry <= this.REFRESH_THRESHOLD_MS) {
        console.log('AuthService: Token expiring soon, attempting silent refresh');
        this.attemptSilentRefresh();
      }
    }, this.CHECK_INTERVAL_MS);
  }

  /**
   * US 3.2.3: Stop token expiry monitoring
   */
  private stopTokenExpiryMonitoring(): void {
    if (this.tokenExpiryCheckInterval) {
      clearInterval(this.tokenExpiryCheckInterval);
      this.tokenExpiryCheckInterval = null;
    }
  }

  /**
   * US 3.2.3: Attempt to silently refresh the token
   * Uses hidden iframe with prompt=none to get new token without user interaction
   */
  private attemptSilentRefresh(): void {
    // Build OAuth URL with prompt=none for silent refresh
    const params = new URLSearchParams({
      client_id: environment.oauth.clientId,
      redirect_uri: environment.oauth.redirectUri,
      response_type: 'id_token token',
      scope: environment.oauth.scope,
      prompt: 'none', // Don't show login UI
      nonce: this.generateNonce(),
      state: 'silent_refresh'
    });

    const authUrl = `${this.GOOGLE_AUTH_URL}?${params.toString()}`;

    // Create hidden iframe for silent authentication
    const iframe = document.createElement('iframe');
    iframe.style.display = 'none';
    iframe.src = authUrl;

    // Listen for messages from iframe
    const messageHandler = (event: MessageEvent) => {
      // Security: verify origin
      if (event.origin !== window.location.origin) {
        return;
      }

      if (event.data.type === 'auth_success') {
        console.log('AuthService: Silent refresh successful');
        this.updateTokens(event.data.id_token, event.data.access_token);
      } else if (event.data.type === 'auth_error') {
        console.warn('AuthService: Silent refresh failed, user will need to re-login');
        // Don't force logout immediately, let token expire naturally
      }

      // Cleanup
      window.removeEventListener('message', messageHandler);
      document.body.removeChild(iframe);
    };

    window.addEventListener('message', messageHandler);
    document.body.appendChild(iframe);

    // Timeout after 10 seconds
    setTimeout(() => {
      if (iframe.parentNode) {
        window.removeEventListener('message', messageHandler);
        document.body.removeChild(iframe);
        console.warn('AuthService: Silent refresh timed out');
      }
    }, 10000);
  }

  /**
   * US 3.2.3: Update stored tokens after silent refresh
   */
  private updateTokens(idToken: string, accessToken: string): void {
    sessionStorage.setItem(this.TOKEN_KEY, idToken);
    sessionStorage.setItem('google_access_token', accessToken);

    // Update expiry time
    const decoded = this.decodeIdToken(idToken);
    if (decoded && decoded.exp) {
      sessionStorage.setItem(this.TOKEN_EXPIRY_KEY, decoded.exp.toString());
    }

    console.log('AuthService: Tokens updated successfully');
  }

  /**
   * US 3.2.3: Handle expired token - force re-login
   */
  private handleExpiredToken(): void {
    console.error('AuthService: Token expired, forcing logout');
    this.logout();
  }

  /**
   * US 3.2.3: Get time until token expiry in milliseconds
   * Returns negative value if already expired
   */
  getTokenExpiryTime(): number {
    const token = sessionStorage.getItem(this.TOKEN_KEY);
    if (!token) {
      return -1;
    }

    const decoded = this.decodeIdToken(token);
    if (!decoded || !decoded.exp) {
      return -1;
    }

    const expirationTime = decoded.exp * 1000;
    return expirationTime - Date.now();
  }

  /**
   * US 3.2.3: Check if token is expiring soon (within refresh threshold)
   */
  isTokenExpiringSoon(): boolean {
    const timeUntilExpiry = this.getTokenExpiryTime();
    return timeUntilExpiry > 0 && timeUntilExpiry <= this.REFRESH_THRESHOLD_MS;
  }
}
