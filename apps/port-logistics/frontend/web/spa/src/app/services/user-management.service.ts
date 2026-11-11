import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SystemUser {
  email: string;
  name: string;
  role: string;
  active: boolean;
  createdAt: string;
  createdBy: string;
  hasPendingActivation: boolean;
  activationTokenExpiry: string | null;
}

export interface CreateUserRequest {
  email: string;
  name: string;
  role: string;
}

export interface UpdateUserRequest {
  email: string;
  name?: string;
  role?: string;
  active?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class UserManagementService {
  private apiUrl = `${environment.apiUrl}/api/usermanagement`;

  constructor(private http: HttpClient) {}

  /**
   * Get all system users (Administrator only)
   */
  getAllUsers(): Observable<SystemUser[]> {
    return this.http.get<SystemUser[]>(this.apiUrl);
  }

  /**
   * Get user by email
   */
  getUserByEmail(email: string): Observable<SystemUser> {
    return this.http.get<SystemUser>(`${this.apiUrl}/${encodeURIComponent(email)}`);
  }

  /**
   * Create a new user (Administrator only)
   * US 3.2.5: Administrator assigns internal role
   */
  createUser(user: CreateUserRequest, createdBy: string): Observable<SystemUser> {
    const params = new HttpParams().set('createdBy', createdBy);
    return this.http.post<SystemUser>(this.apiUrl, user, { params });
  }

  /**
   * Update an existing user (Administrator only)
   * US 3.2.5: Administrator can update roles
   */
  updateUser(user: UpdateUserRequest): Observable<SystemUser> {
    return this.http.put<SystemUser>(this.apiUrl, user);
  }

  /**
   * Resend activation email to user
   * US 3.2.5: Resend unique activation link
   */
  resendActivationEmail(email: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${encodeURIComponent(email)}/resend-activation`, {});
  }

  /**
   * Activate user account with token
   * US 3.2.6: User clicks activation link from email
   */
  /**
   * Validate activation token status
   * US 3.2.6: Check if token is valid, already used, or expired before requiring authentication
   */
  validateActivationToken(token: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/validate-token/${token}`);
  }

  activateUser(email: string, token: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/activate`, { email, activationToken: token });
  }

  /**
   * Get available roles
   * US 3.2.5: Internal roles determine system access level
   */
  getAvailableRoles(): string[] {
    return [
      'Administrator',
      'PortAuthorityOfficer',
      'LogisticsOperator',
      'PortOperationsSupervisor',
      'ShippingAgentRepresentative'
    ];
  }
}
