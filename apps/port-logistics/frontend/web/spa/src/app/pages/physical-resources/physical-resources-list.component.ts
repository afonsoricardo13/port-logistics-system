import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PhysicalResourceManagementService, PhysicalResourceDTO } from '../../services/physical-resource-management.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-physical-resources-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './physical-resources-list.component.html',
  styleUrls: ['./physical-resources-list.component.scss']
})
export class PhysicalResourcesListComponent implements OnInit {
  resources: PhysicalResourceDTO[] = [];
  filteredResources: PhysicalResourceDTO[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // User information
  userRole = '';

  // Filter properties
  searchCode = '';
  searchDescription = '';
  selectedType = '';
  selectedStatus = '';

  // Available types and statuses
  availableTypes = ['STS_CRANE', 'YARD_CRANE', 'TRUCK'];
  availableStatuses = ['Active', 'Inactive', 'UnderMaintenance'];

  constructor(
    private resourceService: PhysicalResourceManagementService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadResources();
  }

  private loadUserInfo(): void {
    const role = this.authService.getUserRole();
    if (role) {
      this.userRole = role;
    }
  }

  loadResources(): void {
    this.loading = true;
    this.error = null;

    this.resourceService.getAll().subscribe({
      next: (resources) => {
        this.resources = resources;
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading resources:', err);
        this.error = 'Failed to load physical resources. Please try again.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.resources];

    if (this.searchCode.trim()) {
      const search = this.searchCode.toLowerCase();
      filtered = filtered.filter(resource => 
        resource.code.toLowerCase().includes(search)
      );
    }

    if (this.searchDescription.trim()) {
      const search = this.searchDescription.toLowerCase();
      filtered = filtered.filter(resource => 
        resource.description.toLowerCase().includes(search)
      );
    }

    if (this.selectedType) {
      filtered = filtered.filter(resource => resource.type?.toUpperCase() === this.selectedType.toUpperCase());
    }

    if (this.selectedStatus) {
      filtered = filtered.filter(resource => 
        resource.status?.toLowerCase() === this.selectedStatus.toLowerCase()
      );
    }

    this.filteredResources = filtered;
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchCode = '';
    this.searchDescription = '';
    this.selectedType = '';
    this.selectedStatus = '';
    this.applyFilters();
  }

  createNew(): void {
    this.router.navigate(['/physical-resources/new']);
  }

  editResource(resource: PhysicalResourceDTO): void {
    this.router.navigate(['/physical-resources', resource.code, 'edit']);
  }

  deactivate(code: string): void {
    if (!confirm(`Are you sure you want to deactivate resource ${code}?`)) {
      return;
    }

    const userProfile = this.authService.getUserProfile();
    const userEmail = userProfile?.email;
    
    this.resourceService.deactivate(code, userEmail).subscribe({
      next: () => {
        this.successMessage = `Resource ${code} deactivated successfully!`;
        setTimeout(() => this.successMessage = null, 3000);
        this.loadResources();
      },
      error: (err) => {
        console.error('Error deactivating resource:', err);
        this.error = 'Failed to deactivate resource.';
      }
    });
  }

  reactivate(code: string): void {
    if (!confirm(`Are you sure you want to reactivate resource ${code}?`)) {
      return;
    }

    const userProfile = this.authService.getUserProfile();
    const userEmail = userProfile?.email;
    
    this.resourceService.reactivate(code, userEmail).subscribe({
      next: () => {
        this.successMessage = `Resource ${code} reactivated successfully!`;
        setTimeout(() => this.successMessage = null, 3000);
        this.loadResources();
      },
      error: (err) => {
        console.error('Error reactivating resource:', err);
        this.error = 'Failed to reactivate resource.';
      }
    });
  }

  getStatusClass(status?: string): string {
    return this.resourceService.getStatusClass(status);
  }

  getStatusLabel(status?: string): string {
    return this.resourceService.getStatusLabel(status);
  }

  canManage(): boolean {
    return this.userRole === 'LogisticsOperator' || this.userRole === 'Administrator';
  }
}
