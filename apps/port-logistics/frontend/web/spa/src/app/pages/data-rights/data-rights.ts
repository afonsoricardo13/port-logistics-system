import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { 
  DataRightsService, 
  DataRightsRequestDto, 
  CreateDataAccessRequestDto,
  CreateDataRectificationRequestDto,
  CreateDataDeletionRequestDto,
  UserDataExportDto
} from '../../services/data-rights.service';
import { AuthService } from '../../services/auth.service';

/**
 * Data Rights Component (US 4.5.3).
 * Provides system users access to their GDPR data rights:
 * - Right of Access (download personal data)
 * - Right to Rectification (request data correction)
 * - Right to Erasure (request data deletion)
 * 
 * Implements RGPD Articles 15-17 and 20 (data portability).
 */
@Component({
  selector: 'app-data-rights',
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './data-rights.html',
  styleUrl: './data-rights.scss',
})
export class DataRights implements OnInit {
  // User requests history
  requests: DataRightsRequestDto[] = [];
  loading = true;
  error: string | null = null;
  successMessage: string | null = null;

  // Modal states
  showExportModal = false;
  showRectificationModal = false;
  showDeletionModal = false;

  // Export options
  exportFormat = 'json';
  exportPreview: UserDataExportDto | null = null;
  loadingExport = false;

  // Rectification form
  rectificationFields: { [key: string]: string } = {};
  rectificationReason = '';
  availableFields = [
    { key: 'name', label: 'Name' },
    { key: 'email', label: 'Email' },
    { key: 'role', label: 'Role' },
    { key: 'organization', label: 'Organization' }
  ];
  selectedFieldsForRectification: string[] = [];

  // Deletion form
  deletionReason = '';

  // Processing states
  submitting = false;

  constructor(
    private dataRightsService: DataRightsService,
    private authService: AuthService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadRequests();
  }

  /**
   * Loads user's data rights requests history.
   */
  loadRequests(): void {
    this.loading = true;
    this.error = null;

    this.dataRightsService.getMyRequests().subscribe({
      next: (requests) => {
        this.requests = requests;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading requests:', err);
        this.error = 'Failed to load your data rights requests. Please try again.';
        this.loading = false;
      }
    });
  }

  /**
   * Opens the data export modal.
   */
  openExportModal(): void {
    this.showExportModal = true;
    this.loadExportPreview();
  }

  /**
   * Loads preview of data to be exported.
   */
  loadExportPreview(): void {
    this.loadingExport = true;
    this.dataRightsService.getExportPreview().subscribe({
      next: (preview) => {
        this.exportPreview = preview;
        this.loadingExport = false;
      },
      error: (err) => {
        console.error('Error loading export preview:', err);
        this.loadingExport = false;
        // Still allow export even if preview fails
      }
    });
  }

  /**
   * Submits a data access request and triggers download.
   */
  submitAccessRequest(): void {
    this.submitting = true;
    this.error = null;

    // First create the request for logging
    const dto: CreateDataAccessRequestDto = { preferredFormat: this.exportFormat };
    
    this.dataRightsService.createAccessRequest(dto).subscribe({
      next: () => {
        // Now download the data
        this.dataRightsService.exportMyData(this.exportFormat).subscribe({
          next: (blob) => {
            this.downloadFile(blob, `my-data-export.${this.exportFormat}`);
            this.showExportModal = false;
            this.submitting = false;
            this.successMessage = 'Your data has been exported successfully. The request has been logged.';
            this.loadRequests();
            setTimeout(() => this.successMessage = null, 5000);
          },
          error: (err) => {
            console.error('Error downloading data:', err);
            this.error = 'Failed to download your data. Please try again.';
            this.submitting = false;
          }
        });
      },
      error: (err) => {
        console.error('Error creating access request:', err);
        this.error = err.error?.message || 'Failed to create data access request. Please try again.';
        this.submitting = false;
      }
    });
  }

  /**
   * Downloads a blob as a file.
   */
  private downloadFile(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  /**
   * Opens the rectification modal.
   */
  openRectificationModal(): void {
    this.showRectificationModal = true;
    this.rectificationFields = {};
    this.rectificationReason = '';
    this.selectedFieldsForRectification = [];
  }

  /**
   * Toggles a field for rectification.
   */
  toggleFieldForRectification(fieldKey: string): void {
    const index = this.selectedFieldsForRectification.indexOf(fieldKey);
    if (index > -1) {
      this.selectedFieldsForRectification.splice(index, 1);
      delete this.rectificationFields[fieldKey];
    } else {
      this.selectedFieldsForRectification.push(fieldKey);
      this.rectificationFields[fieldKey] = '';
    }
  }

  /**
   * Submits a data rectification request.
   */
  submitRectificationRequest(): void {
    if (this.selectedFieldsForRectification.length === 0) {
      this.error = 'Please select at least one field to rectify.';
      return;
    }

    this.submitting = true;
    this.error = null;

    const dto: CreateDataRectificationRequestDto = {
      fieldsToRectify: this.rectificationFields,
      reason: this.rectificationReason || undefined
    };

    this.dataRightsService.createRectificationRequest(dto).subscribe({
      next: () => {
        this.showRectificationModal = false;
        this.submitting = false;
        this.successMessage = 'Your rectification request has been submitted. An administrator will review it.';
        this.loadRequests();
        setTimeout(() => this.successMessage = null, 5000);
      },
      error: (err) => {
        console.error('Error creating rectification request:', err);
        this.error = err.error?.message || 'Failed to submit rectification request. Please try again.';
        this.submitting = false;
      }
    });
  }

  /**
   * Opens the deletion modal.
   */
  openDeletionModal(): void {
    this.showDeletionModal = true;
    this.deletionReason = '';
  }

  /**
   * Submits a data deletion request.
   */
  submitDeletionRequest(): void {
    this.submitting = true;
    this.error = null;

    const dto: CreateDataDeletionRequestDto = {
      reason: this.deletionReason || undefined
    };

    this.dataRightsService.createDeletionRequest(dto).subscribe({
      next: () => {
        this.showDeletionModal = false;
        this.submitting = false;
        this.successMessage = 'Your deletion request has been submitted. An administrator will review it.';
        this.loadRequests();
        setTimeout(() => this.successMessage = null, 5000);
      },
      error: (err) => {
        console.error('Error creating deletion request:', err);
        this.error = err.error?.message || 'Failed to submit deletion request. Please try again.';
        this.submitting = false;
      }
    });
  }

  /**
   * Closes all modals.
   */
  closeModals(): void {
    this.showExportModal = false;
    this.showRectificationModal = false;
    this.showDeletionModal = false;
    this.error = null;
  }

  /**
   * Formats date for display.
   */
  formatDate(dateString: string): string {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-PT', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Gets CSS class for status badge.
   */
  getStatusClass(status: number): string {
    switch (status) {
      case 0: return 'badge-warning'; // Pending
      case 1: return 'badge-info'; // In Progress
      case 2: return 'badge-success'; // Completed
      case 3: return 'badge-danger'; // Rejected
      default: return 'badge-secondary';
    }
  }

  /**
   * Gets CSS class for request type badge.
   */
  getTypeClass(type: number): string {
    switch (type) {
      case 0: return 'badge-primary'; // Access
      case 1: return 'badge-warning'; // Rectification
      case 2: return 'badge-danger'; // Deletion
      default: return 'badge-secondary';
    }
  }
}
