import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { PrivacyPolicyService, PrivacyPolicyDto } from '../../services/privacy-policy.service';
import { AuthService } from '../../services/auth.service';

/**
 * Privacy Policy Change Notification Component
 * US 4.5.1 AC: "A change in the policy must trigger a system user notification upon next login"
 * 
 * Checks on app initialization if there's a new privacy policy that the user hasn't acknowledged.
 * Shows a modal notification that must be dismissed before continuing.
 */
@Component({
  selector: 'app-privacy-policy-notification',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './privacy-policy-notification.html',
  styleUrl: './privacy-policy-notification.scss',
})
export class PrivacyPolicyNotification implements OnInit, OnDestroy {
  showNotification = false;
  currentPolicy: PrivacyPolicyDto | null = null;
  loading = false;
  private lastCheckedUser: string | null = null;
  private checkInterval: Subscription | null = null;

  constructor(
    private privacyPolicyService: PrivacyPolicyService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Initial check
    this.checkForPolicyChanges();

    // Poll every 2 seconds to detect login changes
    this.checkInterval = interval(2000).subscribe(() => {
      const currentUser = this.authService.getUserProfile()?.email || null;
      
      // If user changed (logged in or switched user)
      if (currentUser !== this.lastCheckedUser) {
        this.lastCheckedUser = currentUser;
        
        if (currentUser) {
          // User just logged in - check policy
          this.checkForPolicyChanges();
        } else {
          // User logged out - hide notification
          this.showNotification = false;
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.checkInterval?.unsubscribe();
  }

  private checkForPolicyChanges(): void {
    this.loading = true;

    this.privacyPolicyService.getCurrentPolicy().subscribe({
      next: (policy) => {
        if (!policy) {
          this.loading = false;
          return;
        }

        // Check if user has already seen this exact policy (version + activation date)
        const lastSeenInfo = this.getLastSeenPolicyInfo();
        
        // Show notification if:
        // 1. User never saw any policy, OR
        // 2. User saw a different version, OR
        // 3. User saw same version but with different activation date (reactivated policy)
        if (!lastSeenInfo || 
            lastSeenInfo.version !== policy.version ||
            lastSeenInfo.effectiveDate !== policy.effectiveDate) {
          this.currentPolicy = policy;
          this.showNotification = true;
        }

        this.loading = false;
      },
      error: (err) => {
        console.error('Error checking privacy policy:', err);
        this.loading = false;
      }
    });
  }

  acknowledgePolicy(): void {
    if (this.currentPolicy) {
      this.saveLastSeenPolicyInfo(this.currentPolicy.version, this.currentPolicy.effectiveDate);
      this.showNotification = false;
    }
  }

  private getLastSeenPolicyInfo(): { version: number; effectiveDate: string } | null {
    const userEmail = this.authService.getUserProfile()?.email;
    if (!userEmail) return null;

    const stored = localStorage.getItem(`privacyPolicy_${userEmail}`);
    if (!stored) return null;

    try {
      return JSON.parse(stored);
    } catch {
      // Handle old format (just version number)
      return null;
    }
  }

  private saveLastSeenPolicyInfo(version: number, effectiveDate: string): void {
    const userEmail = this.authService.getUserProfile()?.email;
    if (!userEmail) return;

    const info = { version, effectiveDate };
    localStorage.setItem(`privacyPolicy_${userEmail}`, JSON.stringify(info));
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-PT', { 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric' 
    });
  }
}
