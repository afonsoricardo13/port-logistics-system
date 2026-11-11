import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { VesselVisitNotificationService, VesselVisitNotificationDTO, VesselVisitNotificationFilterDTO } from '../../../services/vessel-visit-notification.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-vessel-visit-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './vvn-list.component.html',
  styleUrls: ['./vvn-list.component.scss']
})
export class VvnListComponent implements OnInit {
  notifications: VesselVisitNotificationDTO[] = [];
  filteredNotifications: VesselVisitNotificationDTO[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // User information
  userRole = '';
  userEmail = '';

  // Filter properties (US 2.2.10 - AC: Support filtering and searching)
  filters: VesselVisitNotificationFilterDTO = {};
  searchText = '';
  selectedStatus = '';
  dateFrom = '';
  dateTo = '';

  // Status options for dropdown
  statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'InProgress', label: 'In Progress' },
    { value: 'Submitted', label: 'Submitted' },
    { value: 'Approved', label: 'Approved' },
    { value: 'Rejected', label: 'Rejected' }
  ];

  constructor(
    private vesselVisitService: VesselVisitNotificationService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadNotifications();
  }

  /**
   * Load current user information from auth service
   */
  private loadUserInfo(): void {
    const profile = this.authService.getUserProfile();
    const role = this.authService.getUserRole();
    
    if (profile) {
      this.userEmail = profile.email || '';
    }
    if (role) {
      this.userRole = role;
    }
  }

  /**
   * Load all vessel visit notifications
   * US 2.2.10 - Retrieve and display notifications
   * - ShippingAgentRepresentative: sees VVNs from their organization
   * - PortAuthorityOfficer/Administrator: sees ALL VVNs
   */
  loadNotifications(): void {
    this.loading = true;
    this.error = null;

    // US 2.2.10: Representatives see only their organization's VVNs
    const isRepresentative = this.userRole === 'ShippingAgentRepresentative';

    if (isRepresentative) {
      // For representatives: get organization notifications (all VVNs from same org)
      this.vesselVisitService.getOrganizationNotifications().subscribe({
        next: (notifications) => {
          this.notifications = notifications;
          this.applyFiltersAndSearch();
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading organization notifications:', err);
          this.error = 'Failed to load vessel visit notifications. Please try again.';
          this.loading = false;
        }
      });
    } else {
      // For Port Officers/Administrators: get all VVNs with filters
      const filters: VesselVisitNotificationFilterDTO = {};
      
      if (this.searchText.trim()) {
        filters.vesselImoNumber = this.searchText;
      }
      if (this.selectedStatus) {
        filters.status = this.selectedStatus;
      }
      if (this.dateFrom) {
        filters.etaFrom = this.dateFrom;
      }
      if (this.dateTo) {
        filters.etaTo = this.dateTo;
      }

      this.vesselVisitService.getAll(filters).subscribe({
        next: (notifications) => {
          this.notifications = notifications;
          this.filteredNotifications = notifications;
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading vessel visit notifications:', err);
          this.error = 'Failed to load vessel visit notifications. Please try again.';
          this.loading = false;
        }
      });
    }
  }

  /**
   * Apply client-side filters (status, date) and search for Representatives
   * US 2.2.10 - Representatives need client-side filtering after getting org notifications
   */
  applyFiltersAndSearch(): void {
    let filtered = [...this.notifications];

    // Filter by status if selected
    if (this.selectedStatus) {
      filtered = filtered.filter(n => n.status === this.selectedStatus);
    }

    // Filter by date range if specified
    if (this.dateFrom) {
      const fromDate = new Date(this.dateFrom);
      filtered = filtered.filter(n => {
        if (!n.estimatedTimeOfArrival) return false;
        const eta = new Date(n.estimatedTimeOfArrival);
        return eta >= fromDate;
      });
    }
    if (this.dateTo) {
      const toDate = new Date(this.dateTo);
      filtered = filtered.filter(n => {
        if (!n.estimatedTimeOfArrival) return false;
        const eta = new Date(n.estimatedTimeOfArrival);
        return eta <= toDate;
      });
    }

    // Apply search text
    if (this.searchText.trim()) {
      const search = this.searchText.toLowerCase();
      filtered = filtered.filter(n => 
        n.vesselImoNumber.toLowerCase().includes(search) ||
        n.assignedDockCode?.toLowerCase().includes(search) ||
        n.organizationIdentifier?.toLowerCase().includes(search) ||
        (n.id && n.id.toString().includes(search))
      );
    }

    this.filteredNotifications = filtered;
  }



  onSearchChange(): void {
    const isRepresentative = this.userRole === 'ShippingAgentRepresentative';
    if (isRepresentative) {
      this.applyFiltersAndSearch();
    } else {
      this.loadNotifications();
    }
  }

  /**
   * Handle filter change and reload data
   */
  onFilterChange(): void {
    const isRepresentative = this.userRole === 'ShippingAgentRepresentative';
    if (isRepresentative) {
      // For representatives: apply client-side filters without reload
      this.applyFiltersAndSearch();
    } else {
      // For officers: reload from server with filters
      this.loadNotifications();
    }
  }

  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.searchText = '';
    this.selectedStatus = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.filters = {};
    this.loadNotifications();
  }

  /**
   * Navigate to create new vessel visit notification
   * US 2.2.8
   */
  createNewNotification(): void {
    this.router.navigate(['/vessel-visits/create']);
  }

  /**
   * View details of a notification
   */
  viewDetails(id: number): void {
    this.router.navigate(['/vessel-visits', id]);
  }

  /**
   * Edit a notification (only if InProgress)
   * US 2.2.9
   */
  editNotification(notification: VesselVisitNotificationDTO): void {
    if (notification.status === 'InProgress') {
      this.router.navigate(['/vessel-visits/edit', notification.id]);
    }
  }

  /**
   * Submit a notification for approval (InProgress -> Submitted)
   * US 2.2.9
   */
  submitForApproval(notification: VesselVisitNotificationDTO): void {
    if (!notification.id) return;
    if (!confirm(`Submit notification for vessel ${notification.vesselImoNumber} for approval?`)) {
      return;
    }

    this.vesselVisitService.submit(notification.id!).subscribe({
      next: () => {
        this.successMessage = `Notification submitted successfully for vessel ${notification.vesselImoNumber}`;
        setTimeout(() => this.successMessage = null, 5000);
        this.loadNotifications();
      },
      error: (err) => {
        console.error('Error submitting notification:', err);
        this.error = err.error?.join(', ') || 'Failed to submit notification.';
      }
    });
  }

  /**
   * Approve a notification (Port Authority Officer only)
   * US 2.2.7
   */
  approveNotification(notification: VesselVisitNotificationDTO): void {
    if (!notification.id) return;
    const dockCode = prompt(`Enter dock code to assign to vessel ${notification.vesselImoNumber}:`);
    if (!dockCode) return;

    const officerId = this.userEmail; // Use email as officer ID
    if (!officerId) {
      this.error = 'Officer ID is required.';
      return;
    }

    this.vesselVisitService.approve(notification.id!, officerId, dockCode).subscribe({
      next: () => {
        this.successMessage = `Notification approved and assigned to dock ${dockCode}`;
        setTimeout(() => this.successMessage = null, 5000);
        this.loadNotifications();
      },
      error: (err) => {
        console.error('Error approving notification:', err);
        this.error = err.error?.join(', ') || 'Failed to approve notification.';
      }
    });
  }

  /**
   * Reject a notification (Port Authority Officer only)
   * US 2.2.7
   */
  rejectNotification(notification: VesselVisitNotificationDTO): void {
    if (!notification.id) return;
    const reason = prompt(`Enter rejection reason for vessel ${notification.vesselImoNumber}:`);
    if (!reason) return;

    const officerId = this.userEmail; // Use email as officer ID
    if (!officerId || !reason) {
      this.error = 'Officer ID and reason are required.';
      return;
    }

    this.vesselVisitService.reject(notification.id!, officerId, reason).subscribe({
      next: () => {
        this.successMessage = `Notification rejected`;
        setTimeout(() => this.successMessage = null, 5000);
        this.loadNotifications();
      },
      error: (err) => {
        console.error('Error rejecting notification:', err);
        this.error = err.error?.join(', ') || 'Failed to reject notification.';
      }
    });
  }

  /**
   * Check if user can edit a notification
   */
  canEdit(notification: VesselVisitNotificationDTO): boolean {
    return notification.status === 'InProgress' && 
           (this.userRole === 'ShippingAgentRepresentative' || this.userRole === 'Administrator');
  }

  /**
   * Check if user can submit a notification
   */
  canSubmit(notification: VesselVisitNotificationDTO): boolean {
    return notification.status === 'InProgress' && 
           (this.userRole === 'ShippingAgentRepresentative' || this.userRole === 'Administrator');
  }

  /**
   * Check if user can approve/reject notifications
   */
  canApproveReject(notification: VesselVisitNotificationDTO): boolean {
    return notification.status === 'Submitted' && 
           (this.userRole === 'PortAuthorityOfficer' || this.userRole === 'Administrator');
  }

  /**
   * Get CSS class for status badge
   * US 2.2.10 - AC: Visual status indicators
   */
  getStatusClass(status: string): string {
    switch (status) {
      case 'InProgress':
        return 'status-in-progress';
      case 'Submitted':
        return 'status-submitted';
      case 'Approved':
        return 'status-approved';
      case 'Rejected':
        return 'status-rejected';
      default:
        return '';
    }
  }

  /**
   * Get display text for status
   */
  getStatusText(status: string): string {
    switch (status) {
      case 'InProgress':
        return 'In Progress';
      case 'Submitted':
        return 'Pending Approval';
      case 'Approved':
        return 'Approved';
      case 'Rejected':
        return 'Rejected';
      default:
        return status;
    }
  }

  /**
   * Format date for display
   */
  formatDate(dateString?: string): string {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Format date for display (short version)
   */
  formatDateShort(dateString?: string): string {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
