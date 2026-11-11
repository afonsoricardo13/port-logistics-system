import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

/**
 * Access Denied Component
 * US 3.2.2 - Display when user has no assigned role or account is inactive
 * 
 * AC: If the user has no assigned role or it is inactive, access must be denied with an appropriate message
 */
@Component({
  selector: 'app-access-denied',
  imports: [CommonModule],
  template: `
    <div class="access-denied-container">
      <div class="access-denied-card">
        <div class="icon">
          <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" fill="currentColor" viewBox="0 0 16 16">
            <path d="M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"/>
            <path d="M4.646 4.646a.5.5 0 0 1 .708 0L8 7.293l2.646-2.647a.5.5 0 0 1 .708.708L8.707 8l2.647 2.646a.5.5 0 0 1-.708.708L8 8.707l-2.646 2.647a.5.5 0 0 1-.708-.708L7.293 8 4.646 5.354a.5.5 0 0 1 0-.708z"/>
          </svg>
        </div>
        
        <h1>Access Denied</h1>
        
        <p class="message">
          You do not have permission to access this system.
        </p>
        
        <div class="user-info" *ngIf="userEmail">
          <p><strong>Account:</strong> {{ userEmail }}</p>
        </div>
        
        <div class="details">
          <p><strong>Reason:</strong></p>
          <p class="reason-text">{{ reason }}</p>
        </div>
        
        <p class="contact">
          Please contact the system administrator if you believe this is an error.
        </p>
        
        <button class="back-button" (click)="goToLogin()">
          Return to Login
        </button>
      </div>
    </div>
  `,
  styles: [`
    .access-denied-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: #2d3748;
      padding: 20px;
    }

    .access-denied-card {
      background: white;
      border-radius: 12px;
      padding: 48px 40px;
      max-width: 500px;
      width: 100%;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.1);
      text-align: center;
    }

    .icon {
      color: #dc3545;
      margin-bottom: 24px;
    }

    h1 {
      color: #dc3545;
      font-size: 32px;
      font-weight: 700;
      margin-bottom: 16px;
    }

    .message {
      color: #6c757d;
      font-size: 18px;
      margin-bottom: 24px;
    }

    .user-info {
      background: #e7f3ff;
      border-left: 4px solid #0066cc;
      padding: 12px 16px;
      border-radius: 4px;
      margin-bottom: 24px;
      text-align: left;
    }

    .user-info p {
      margin: 0;
      color: #495057;
      font-size: 14px;
    }

    .details {
      text-align: left;
      background: #fff3cd;
      border-left: 4px solid #ffc107;
      padding: 20px;
      border-radius: 4px;
      margin-bottom: 24px;
    }

    .details p {
      margin: 0 0 8px 0;
      color: #495057;
      font-weight: 600;
    }

    .reason-text {
      margin: 0 !important;
      color: #856404 !important;
      font-weight: 400 !important;
      font-size: 15px;
    }

    .details ul {
      margin: 0;
      padding-left: 20px;
      color: #6c757d;
    }

    .details li {
      margin-bottom: 8px;
    }

    .contact {
      color: #6c757d;
      font-size: 14px;
      margin-bottom: 32px;
    }

    .back-button {
      background: #2d3748;
      color: white;
      border: none;
      padding: 12px 32px;
      border-radius: 8px;
      font-size: 16px;
      font-weight: 600;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .back-button:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 20px rgba(45, 55, 72, 0.4);
      background: #1a252f;
    }

    .back-button:active {
      transform: translateY(0);
    }
  `]
})
export class AccessDeniedComponent {
  reason: string = '';
  userEmail: string = '';

  constructor(private router: Router) {
    // Get the reason and email from sessionStorage
    this.reason = sessionStorage.getItem('access_denied_reason') || 'Access to this system has been denied.';
    this.userEmail = sessionStorage.getItem('access_denied_email') || '';
  }

  goToLogin(): void {
    // Clear session storage before going to login
    sessionStorage.clear();
    this.router.navigate(['/login']);
  }
}
