import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { VesselVisitNotificationService, VesselVisitNotificationDTO, DecisionLogDTO } from '../../../services/vessel-visit-notification.service';

@Component({
  selector: 'app-vessel-visit-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './vvn-details.component.html',
  styleUrls: ['./vvn-details.component.scss']
})
export class VvnDetailsComponent implements OnInit {
  notification: VesselVisitNotificationDTO | null = null;
  auditTrail: DecisionLogDTO[] = [];
  loading = false;
  error: string | null = null;
  notificationId: number = 0;

  constructor(
    private vesselVisitService: VesselVisitNotificationService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.notificationId = parseInt(id, 10);
      this.loadNotification();
      this.loadAuditTrail();
    }
  }

  loadNotification(): void {
    this.loading = true;
    this.error = null;

    this.vesselVisitService.getById(this.notificationId).subscribe({
      next: (notification) => {
        this.notification = notification;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading vessel visit notification:', err);
        this.error = 'Failed to load vessel visit notification details.';
        this.loading = false;
      }
    });
  }

  loadAuditTrail(): void {
    this.vesselVisitService.getAudit(this.notificationId).subscribe({
      next: (audit) => {
        this.auditTrail = audit;
      },
      error: (err) => {
        console.error('Error loading audit trail:', err);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/vessel-visits']);
  }

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

  formatDate(dateString?: string): string {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
