import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { PrivacyPolicyService, PrivacyPolicyDto } from '../../services/privacy-policy.service';

/**
 * Public Privacy Policy Component (US 4.5.1).
 * Displays the currently active privacy policy to all users (including anonymous).
 * Accessible from footer link: "Política de Privacidade".
 * Implements RGPD Articles 13-14 transparency requirements.
 */
@Component({
  selector: 'app-privacy-policy',
  imports: [CommonModule, TranslateModule],
  templateUrl: './privacy-policy.html',
  styleUrl: './privacy-policy.scss',
})
export class PrivacyPolicy implements OnInit {
  policy: PrivacyPolicyDto | null = null;
  loading = true;
  error: string | null = null;

  constructor(private privacyPolicyService: PrivacyPolicyService) {}

  ngOnInit(): void {
    this.loadCurrentPolicy();
  }

  /**
   * Loads the currently active privacy policy from the backend.
   */
  private loadCurrentPolicy(): void {
    this.loading = true;
    this.error = null;

    this.privacyPolicyService.getCurrentPolicy().subscribe({
      next: (policy) => {
        this.policy = policy;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading privacy policy:', err);
        this.error = 'Failed to load privacy policy. Please try again later.';
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
}
