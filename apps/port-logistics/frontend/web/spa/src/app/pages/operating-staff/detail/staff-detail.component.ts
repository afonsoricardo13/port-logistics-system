import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { OperatingStaffService, OperatingStaffDTO } from '../../../services/operating-staff.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-staff-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './staff-detail.component.html',
  styleUrl: './staff-detail.component.scss'
})
export class StaffDetailComponent implements OnInit {
  private operatingStaffService = inject(OperatingStaffService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);

  staffMember: OperatingStaffDTO | null = null;
  mecanographicNumber: string = '';
  loading: boolean = false;
  error: string | null = null;
  successMessage: string | null = null;

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['number']) {
        this.mecanographicNumber = params['number'];
        this.loadStaffMember();
      } else {
        this.error = 'No mecanographic number provided.';
      }
    });
  }

  loadStaffMember(): void {
    if (!this.mecanographicNumber) return;

    this.loading = true;
    this.error = null;

    this.operatingStaffService.getByMecanographicNumber(this.mecanographicNumber).subscribe({
      next: (staff: OperatingStaffDTO) => {
        this.staffMember = staff;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading staff member:', err);
        this.error = err.error?.message || 'Failed to load staff member details. Please try again.';
        this.loading = false;
      }
    });
  }

  onDeactivate(): void {
    if (!this.staffMember) return;

    const reason = prompt('Please provide a reason for deactivating this staff member:');
    
    if (!reason) {
      return; // User cancelled
    }

    if (!confirm(`Are you sure you want to deactivate ${this.staffMember.name}?`)) {
      return;
    }

    this.operatingStaffService.deactivate(this.staffMember.mecanographicNumber, reason).subscribe({
      next: (updatedStaff: OperatingStaffDTO) => {
        this.staffMember = updatedStaff;
        this.successMessage = `${updatedStaff.name} has been deactivated successfully.`;
        
        // Clear success message after 5 seconds
        setTimeout(() => {
          this.successMessage = null;
        }, 5000);
      },
      error: (err) => {
        console.error('Error deactivating staff:', err);
        this.error = err.error?.message || 'Failed to deactivate staff member. Please try again.';
      }
    });
  }

  onReactivate(): void {
    if (!this.staffMember) return;

    const reason = prompt('Please provide a reason for reactivating this staff member:');
    
    if (!reason) {
      return; // User cancelled
    }

    if (!confirm(`Are you sure you want to reactivate ${this.staffMember.name}?`)) {
      return;
    }

    this.operatingStaffService.reactivate(this.staffMember.mecanographicNumber, reason).subscribe({
      next: (updatedStaff: OperatingStaffDTO) => {
        this.staffMember = updatedStaff;
        this.successMessage = `${updatedStaff.name} has been reactivated successfully.`;
        
        // Clear success message after 5 seconds
        setTimeout(() => {
          this.successMessage = null;
        }, 5000);
      },
      error: (err) => {
        console.error('Error reactivating staff:', err);
        this.error = err.error?.message || 'Failed to reactivate staff member. Please try again.';
      }
    });
  }

  onEdit(): void {
    if (!this.staffMember) return;
    this.router.navigate(['/operating-staff/edit', this.staffMember.mecanographicNumber]);
  }

  onBack(): void {
    this.router.navigate(['/operating-staff']);
  }

  canManageStaff(): boolean {
    const userRole = this.authService.getUserRole();
    return userRole === 'LogisticsOperator';
  }

  get formattedOperationalWindow(): string {
    if (!this.staffMember?.operationalWindow) return 'N/A';
    return this.staffMember.operationalWindow;
  }

  get statusClass(): string {
    if (!this.staffMember) return '';
    return this.staffMember.status.toLowerCase();
  }
}
