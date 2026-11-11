import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { 
  VesselVisitNotificationService, 
  VesselVisitNotificationDTO,
  DecisionLogDTO 
} from '../../../services/vessel-visit-notification.service';
import { AuthService } from '../../../services/auth.service';
import { DockManagementService, DockDTO } from '../../../services/dock-management.service';

/**
 * Review Component for Vessel Visit Notifications
 * 
 * US 2.2.7: Port Officer reviews and approves/rejects notifications
 * - Assign dock on approval
 * - Provide rejection reason on rejection
 * - View decision audit trail
 */
@Component({
  selector: 'app-vvn-review',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './vvn-review.component.html',
  styleUrls: ['./vvn-review.component.scss']
})
export class VvnReviewComponent implements OnInit {
  notification: VesselVisitNotificationDTO | null = null;
  auditTrail: DecisionLogDTO[] = [];
  notificationId!: number;
  loading = false;
  submitting = false;
  error: string | null = null;
  successMessage: string | null = null;

  // Modals
  showApproveModal = false;
  showRejectModal = false;

  // Forms
  approveForm!: FormGroup;
  rejectForm!: FormGroup;

  // Available docks (loaded from API)
  availableDocks: DockDTO[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private vvnService: VesselVisitNotificationService,
    private authService: AuthService,
    private dockService: DockManagementService
  ) {
    this.initializeForms();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.notificationId = +id;
      this.loadNotification();
      this.loadAuditTrail();
      this.loadDocks();
    } else {
      this.error = 'Invalid notification ID';
    }
  }

  private initializeForms(): void {
    this.approveForm = this.fb.group({
      assignedDockCode: ['', Validators.required]
    });

    this.rejectForm = this.fb.group({
      rejectionReason: ['', [Validators.required, Validators.minLength(10)]]
    });
  }

  private loadNotification(): void {
    this.loading = true;
    this.error = null;

    this.vvnService.getById(this.notificationId).subscribe({
      next: (data) => {
        this.notification = data;
        this.loading = false;

        // Check if notification can be reviewed
        if (data.status !== 'Submitted') {
          this.error = `This notification cannot be reviewed. Current status: ${data.status}`;
        }
      },
      error: (err) => {
        this.error = 'Failed to load notification';
        console.error('Error loading notification:', err);
        this.loading = false;
      }
    });
  }

  private loadAuditTrail(): void {
    this.vvnService.getAudit(this.notificationId).subscribe({
      next: (data) => {
        this.auditTrail = data;
      },
      error: (err) => {
        console.error('Error loading audit trail:', err);
      }
    });
  }

  private loadDocks(): void {
    this.dockService.getAll().subscribe({
      next: (docks) => {
        this.availableDocks = docks;
      },
      error: (err) => {
        console.error('Error loading docks:', err);
        this.error = 'Failed to load available docks';
      }
    });
  }

  openApproveModal(): void {
    if (this.notification?.preferredDockCode) {
      this.approveForm.patchValue({
        assignedDockCode: this.notification.preferredDockCode
      });
    }
    this.showApproveModal = true;
  }

  openRejectModal(): void {
    this.showRejectModal = true;
  }

  closeApproveModal(): void {
    this.showApproveModal = false;
    this.approveForm.reset();
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.rejectForm.reset();
  }

  approve(): void {
    if (this.approveForm.invalid) {
      this.approveForm.markAllAsTouched();
      return;
    }

    const currentUser = this.authService.getUserProfile();
    if (!currentUser?.email) {
      this.error = 'User not authenticated';
      return;
    }

    this.submitting = true;
    this.error = null;
    const dockCode = this.approveForm.get('assignedDockCode')?.value;

    this.vvnService.approve(this.notificationId, currentUser.email, dockCode).subscribe({
      next: () => {
        this.handleSuccess('Notification approved successfully!');
        this.closeApproveModal();
      },
      error: (err) => {
        this.handleError(err, 'Failed to approve notification');
      }
    });
  }

  reject(): void {
    if (this.rejectForm.invalid) {
      this.rejectForm.markAllAsTouched();
      return;
    }

    const currentUser = this.authService.getUserProfile();
    if (!currentUser?.email) {
      this.error = 'User not authenticated';
      return;
    }

    this.submitting = true;
    this.error = null;
    const reason = this.rejectForm.get('rejectionReason')?.value;

    this.vvnService.reject(this.notificationId, currentUser.email, reason).subscribe({
      next: () => {
        this.handleSuccess('Notification rejected successfully!');
        this.closeRejectModal();
      },
      error: (err) => {
        this.handleError(err, 'Failed to reject notification');
      }
    });
  }

  private handleSuccess(message: string): void {
    this.successMessage = message;
    this.submitting = false;
    this.loadNotification();
    this.loadAuditTrail();
    setTimeout(() => this.router.navigate(['/vessel-visit-notifications']), 2000);
  }

  private handleError(err: any, defaultMsg: string): void {
    let errorMsg = defaultMsg;
    if (err.error) {
      if (Array.isArray(err.error)) {
        errorMsg = err.error.join('; ');
      } else if (typeof err.error === 'string') {
        errorMsg = err.error;
      } else if (err.error.message) {
        errorMsg = err.error.message;
      }
    }
    this.error = errorMsg;
    this.submitting = false;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  goBack(): void {
    this.router.navigate(['/vessel-visit-notifications']);
  }

  getStatusBadgeClass(status: string): string {
    const statusMap: { [key: string]: string } = {
      'InProgress': 'bg-secondary',
      'Submitted': 'bg-primary',
      'Approved': 'bg-success',
      'Rejected': 'bg-danger'
    };
    return statusMap[status] || 'bg-secondary';
  }

  canReview(): boolean {
    return this.notification?.status === 'Submitted';
  }
}
