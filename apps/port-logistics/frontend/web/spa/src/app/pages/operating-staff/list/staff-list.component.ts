import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { OperatingStaffService, OperatingStaffDTO, StaffSearchParams } from '../../../services/operating-staff.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-staff-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './staff-list.component.html',
  styleUrls: ['./staff-list.component.scss']
})
export class StaffListComponent implements OnInit {
  staffMembers: OperatingStaffDTO[] = [];
  filteredStaff: OperatingStaffDTO[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // User information
  userRole = '';
  userEmail = '';

  // Filter properties
  searchText = '';
  selectedStatus = '';
  selectedQualification = '';

  // Status options
  statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'Available', label: 'Available' },
    { value: 'Unavailable', label: 'Unavailable' }
  ];

  constructor(
    private staffService: OperatingStaffService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadStaff();
  }

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

  loadStaff(): void {
    this.loading = true;
    this.error = null;

    const filters: StaffSearchParams = {};
    
    if (this.selectedStatus) {
      filters.status = this.selectedStatus;
    }
    if (this.selectedQualification) {
      filters.qualification = this.selectedQualification;
    }

    this.staffService.getAll(filters).subscribe({
      next: (staff) => {
        this.staffMembers = staff;
        this.applySearchFilter();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading staff:', err);
        this.error = 'Failed to load operating staff. Please try again.';
        this.loading = false;
      }
    });
  }

  applySearchFilter(): void {
    if (!this.searchText.trim()) {
      this.filteredStaff = [...this.staffMembers];
      return;
    }

    const search = this.searchText.toLowerCase();
    this.filteredStaff = this.staffMembers.filter(staff => 
      staff.mecanographicNumber.toLowerCase().includes(search) ||
      staff.name.toLowerCase().includes(search) ||
      staff.email.toLowerCase().includes(search) ||
      staff.qualifications.some(q => q.toLowerCase().includes(search))
    );
  }

  onSearchChange(): void {
    this.applySearchFilter();
  }

  onFilterChange(): void {
    this.loadStaff();
  }

  clearFilters(): void {
    this.searchText = '';
    this.selectedStatus = '';
    this.selectedQualification = '';
    this.loadStaff();
  }

  createNewStaff(): void {
    this.router.navigate(['/operating-staff/create']);
  }

  viewDetails(mecanographicNumber: string): void {
    this.router.navigate(['/operating-staff', mecanographicNumber]);
  }

  editStaff(staff: OperatingStaffDTO): void {
    this.router.navigate(['/operating-staff/edit', staff.mecanographicNumber]);
  }

  deactivateStaff(staff: OperatingStaffDTO): void {
    if (!confirm(`Are you sure you want to deactivate ${staff.name}?`)) {
      return;
    }

    const reason = prompt('Please provide a reason for deactivation (optional):');
    
    this.loading = true;
    this.error = null;

    this.staffService.deactivate(staff.mecanographicNumber, this.userEmail, reason || undefined).subscribe({
      next: (updatedStaff) => {
        // Update the staff member in the list with the response from backend
        const index = this.staffMembers.findIndex(s => s.mecanographicNumber === updatedStaff.mecanographicNumber);
        if (index !== -1) {
          this.staffMembers[index] = updatedStaff;
          this.applySearchFilter();
        }
        this.successMessage = `Staff member ${staff.name} deactivated successfully!`;
        this.loading = false;
        setTimeout(() => this.successMessage = null, 3000);
      },
      error: (err) => {
        console.error('Error deactivating staff:', err);
        const errorMsg = this.extractErrorMessage(err);
        this.error = `Failed to deactivate staff member: ${errorMsg}`;
        this.loading = false;
      }
    });
  }

  reactivateStaff(staff: OperatingStaffDTO): void {
    if (!confirm(`Are you sure you want to reactivate ${staff.name}?`)) {
      return;
    }

    const reason = prompt('Please provide a reason for reactivation (optional):');
    
    this.loading = true;
    this.error = null;

    this.staffService.reactivate(staff.mecanographicNumber, this.userEmail, reason || undefined).subscribe({
      next: (updatedStaff) => {
        // Update the staff member in the list with the response from backend
        const index = this.staffMembers.findIndex(s => s.mecanographicNumber === updatedStaff.mecanographicNumber);
        if (index !== -1) {
          this.staffMembers[index] = updatedStaff;
          this.applySearchFilter();
        }
        this.successMessage = `Staff member ${staff.name} reactivated successfully!`;
        this.loading = false;
        setTimeout(() => this.successMessage = null, 3000);
      },
      error: (err) => {
        console.error('Error reactivating staff:', err);
        const errorMsg = this.extractErrorMessage(err);
        this.error = `Failed to reactivate staff member: ${errorMsg}`;
        this.loading = false;
      }
    });
  }

  canManageStaff(): boolean {
    return this.userRole === 'LogisticsOperator' || this.userRole === 'Administrator';
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Available':
        return 'status-available';
      case 'Unavailable':
        return 'status-unavailable';
      default:
        return '';
    }
  }

  formatQualifications(qualifications: string[]): string {
    return qualifications.join(', ');
  }

  private extractErrorMessage(err: any): string {
    if (err.error) {
      if (err.error.errors && Array.isArray(err.error.errors)) {
        return err.error.errors.join(', ');
      } else if (typeof err.error === 'string') {
        return err.error;
      } else if (err.error.message) {
        return err.error.message;
      }
    }
    return 'An unexpected error occurred';
  }
}
