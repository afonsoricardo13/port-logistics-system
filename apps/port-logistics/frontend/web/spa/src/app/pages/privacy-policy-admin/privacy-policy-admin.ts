import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { 
  PrivacyPolicyService, 
  PrivacyPolicyHistoryItemDto,
  CreatePrivacyPolicyDto,
  PrivacyPolicyDto
} from '../../services/privacy-policy.service';
import { 
  GDPR_PRIVACY_POLICY_TEMPLATE, 
  getDefaultEffectiveDate,
  validateGDPRContent 
} from './gdpr-template';

/**
 * Admin Privacy Policy Management Component (US 4.5.1).
 * Allows administrators to:
 * - Create new privacy policy versions
 * - View policy history
 * - Activate specific versions
 * - Trigger user notifications
 * 
 * Implements GDPR accountability requirements (RGPD Art 5.2).
 */
@Component({
  selector: 'app-privacy-policy-admin',
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './privacy-policy-admin.html',
  styleUrl: './privacy-policy-admin.scss',
})
export class PrivacyPolicyAdmin implements OnInit {
  // State management
  activeTab: 'create' | 'history' = 'history';
  history: PrivacyPolicyHistoryItemDto[] = [];
  selectedPolicy: PrivacyPolicyDto | null = null;
  loading = false;
  historyLoading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // Form model for creating new policy
  newPolicy: CreatePrivacyPolicyDto = {
    content: '',
    effectiveDate: new Date().toISOString().split('T')[0],
    changeNotes: '',
    activateImmediately: true,
    notifyUsers: true
  };

  constructor(private privacyPolicyService: PrivacyPolicyService) {}

  ngOnInit(): void {
    this.loadHistory();
  }

  /**
   * Loads the complete policy history.
   */
  loadHistory(): void {
    this.historyLoading = true;
    this.error = null;

    this.privacyPolicyService.getPolicyHistory().subscribe({
      next: (history) => {
        this.history = history;
        this.historyLoading = false;
      },
      error: (err) => {
        console.error('Error loading policy history:', err);
        this.error = 'Failed to load policy history.';
        this.historyLoading = false;
      }
    });
  }

  /**
   * Switches between tabs (create / history).
   */
  switchTab(tab: 'create' | 'history'): void {
    this.activeTab = tab;
    this.clearMessages();
  }

  /**
   * Creates a new privacy policy version.
   */
  createPolicy(): void {
    this.clearMessages();

    // GDPR Content Validation (US 4.5.2)
    const validation = validateGDPRContent(this.newPolicy.content);
    if (!validation.valid) {
      this.error = 'GDPR Compliance Issues:\n' + validation.errors.join('\n');
      return;
    }

    // Basic validation
    if (!this.newPolicy.content || this.newPolicy.content.length < 500) {
      this.error = 'Policy content must be at least 500 characters to meet GDPR requirements.';
      return;
    }

    if (!this.newPolicy.effectiveDate) {
      this.error = 'Effective date is required.';
      return;
    }

    this.loading = true;

    this.privacyPolicyService.createPolicy(this.newPolicy).subscribe({
      next: (createdPolicy) => {
        this.successMessage = `Privacy Policy version ${createdPolicy.version} created successfully!`;
        this.loading = false;
        
        // Reset form
        this.newPolicy = {
          content: '',
          effectiveDate: getDefaultEffectiveDate(),
          changeNotes: '',
          activateImmediately: true,
          notifyUsers: true
        };

        // Reload history
        this.loadHistory();

        // Switch to history tab to see result
        setTimeout(() => this.switchTab('history'), 1500);
      },
      error: (err) => {
        console.error('Error creating policy:', err);
        this.error = err.error?.message || 'Failed to create privacy policy.';
        this.loading = false;
      }
    });
  }

  /**
   * Views the full content of a specific policy version.
   */
  viewPolicy(version: number): void {
    this.clearMessages();
    this.loading = true;

    this.privacyPolicyService.getPolicyByVersion(version).subscribe({
      next: (policy) => {
        this.selectedPolicy = policy;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading policy:', err);
        this.error = 'Failed to load policy details.';
        this.loading = false;
      }
    });
  }

  /**
   * Closes the policy detail view.
   */
  closeView(): void {
    this.selectedPolicy = null;
    this.clearMessages();
  }

  /**
   * Activates a specific policy version.
   */
  activatePolicy(id: number, version: number): void {
    if (!confirm(`Activate Privacy Policy version ${version}? This will deactivate all other versions.`)) {
      return;
    }

    this.clearMessages();
    this.loading = true;

    this.privacyPolicyService.activatePolicy(id).subscribe({
      next: () => {
        this.successMessage = `Privacy Policy version ${version} activated successfully!`;
        this.loading = false;
        this.loadHistory();
      },
      error: (err) => {
        console.error('Error activating policy:', err);
        this.error = err.error?.message || 'Failed to activate policy.';
        this.loading = false;
      }
    });
  }

  /**
   * Triggers user notifications about policy change.
   */
  notifyUsers(id: number, version: number): void {
    if (!confirm(`Send notification emails to all users about Privacy Policy version ${version}?`)) {
      return;
    }

    this.clearMessages();
    this.loading = true;

    this.privacyPolicyService.notifyUsers(id).subscribe({
      next: () => {
        this.successMessage = `Notification process started for version ${version}!`;
        this.loading = false;
        this.loadHistory();
      },
      error: (err) => {
        console.error('Error notifying users:', err);
        this.error = err.error?.message || 'Failed to trigger notifications.';
        this.loading = false;
      }
    });
  }

  /**
   * Formats date for display (Portuguese format).
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-PT', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  /**
   * Loads the GDPR-compliant template into the content field (US 4.5.2).
   */
  loadGDPRTemplate(): void {
    if (this.newPolicy.content && !confirm('This will replace current content. Continue?')) {
      return;
    }
    
    this.newPolicy.content = GDPR_PRIVACY_POLICY_TEMPLATE;
    this.newPolicy.effectiveDate = getDefaultEffectiveDate();
    this.successMessage = 'GDPR-compliant template loaded successfully. Review and customize as needed.';
    
    // Clear message after 5 seconds
    setTimeout(() => this.clearMessages(), 5000);
  }

  /**
   * Validates if content meets GDPR requirements.
   */
  validateContent(): void {
    this.clearMessages();
    
    const validation = validateGDPRContent(this.newPolicy.content);
    
    if (validation.valid) {
      this.successMessage = '✓ Content meets GDPR minimum requirements (Arts. 13-14)';
    } else {
      this.error = 'GDPR Compliance Issues:\n' + validation.errors.join('\n');
    }
  }

  /**
   * Clears error and success messages.
   */
  private clearMessages(): void {
    this.error = null;
    this.successMessage = null;
  }
}
