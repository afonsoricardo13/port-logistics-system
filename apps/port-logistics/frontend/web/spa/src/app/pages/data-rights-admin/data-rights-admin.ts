import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { 
  DataRightsService, 
  DataRightsRequestDto, 
  ProcessDataRightsRequestDto
} from '../../services/data-rights.service';

/**
 * Data Rights Admin Component (US 4.5.3).
 * Allows administrators to manage GDPR data rights requests:
 * - View all requests
 * - Process requests (approve, reject, mark in progress)
 * - View request details
 */
@Component({
  selector: 'app-data-rights-admin',
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './data-rights-admin.html',
  styleUrl: './data-rights-admin.scss',
})
export class DataRightsAdmin implements OnInit {
  // Requests data
  allRequests: DataRightsRequestDto[] = [];
  filteredRequests: DataRightsRequestDto[] = [];
  loading = true;
  error: string | null = null;
  successMessage: string | null = null;

  // Filtering
  filterStatus: string = 'all';
  filterType: string = 'all';

  // Process Modal
  showProcessModal = false;
  selectedRequest: DataRightsRequestDto | null = null;
  processStatus: number = 2; // Default to Completed
  adminResponse: string = '';
  processing = false;

  // Detail Modal
  showDetailModal = false;

  constructor(
    private dataRightsService: DataRightsService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadRequests();
  }

  /**
   * Loads all data rights requests.
   */
  loadRequests(): void {
    this.loading = true;
    this.error = null;

    this.dataRightsService.getAllRequests().subscribe({
      next: (requests) => {
        this.allRequests = requests;
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading requests:', err);
        this.error = 'Failed to load data rights requests. Please try again.';
        this.loading = false;
      }
    });
  }

  /**
   * Applies status and type filters.
   */
  applyFilters(): void {
    this.filteredRequests = this.allRequests.filter(r => {
      const statusMatch = this.filterStatus === 'all' || r.status.toString() === this.filterStatus;
      const typeMatch = this.filterType === 'all' || r.requestType.toString() === this.filterType;
      return statusMatch && typeMatch;
    });
  }

  /**
   * Opens the process modal for a request.
   */
  openProcessModal(request: DataRightsRequestDto): void {
    this.selectedRequest = request;
    this.processStatus = 2; // Default to Completed
    this.adminResponse = '';
    this.showProcessModal = true;
  }

  /**
   * Opens the detail modal for a request.
   */
  openDetailModal(request: DataRightsRequestDto): void {
    this.selectedRequest = request;
    this.showDetailModal = true;
  }

  /**
   * Closes all modals.
   */
  closeModals(): void {
    this.showProcessModal = false;
    this.showDetailModal = false;
    this.selectedRequest = null;
    this.error = null;
  }

  /**
   * Processes the selected request.
   */
  processRequest(): void {
    if (!this.selectedRequest) return;

    this.processing = true;
    this.error = null;

    const dto: ProcessDataRightsRequestDto = {
      requestId: this.selectedRequest.id,
      newStatus: this.processStatus,
      adminResponse: this.adminResponse || undefined
    };

    this.dataRightsService.processRequest(dto).subscribe({
      next: () => {
        this.showProcessModal = false;
        this.processing = false;
        this.successMessage = `Request #${this.selectedRequest?.id} has been processed successfully.`;
        this.loadRequests();
        setTimeout(() => this.successMessage = null, 5000);
      },
      error: (err) => {
        console.error('Error processing request:', err);
        this.error = err.error?.message || 'Failed to process request. Please try again.';
        this.processing = false;
      }
    });
  }

  /**
   * Formats date for display.
   */
  formatDate(dateString: string | undefined): string {
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

  /**
   * Gets formatted fields for rectification display.
   */
  getFieldsDisplay(fields: { [key: string]: string } | undefined): string {
    if (!fields) return 'N/A';
    return Object.entries(fields)
      .map(([key, value]) => `${key}: "${value}"`)
      .join(', ');
  }

  /**
   * Gets pending count.
   */
  get pendingCount(): number {
    return this.allRequests.filter(r => r.status === 0).length;
  }

  /**
   * Gets in-progress count.
   */
  get inProgressCount(): number {
    return this.allRequests.filter(r => r.status === 1).length;
  }
}
