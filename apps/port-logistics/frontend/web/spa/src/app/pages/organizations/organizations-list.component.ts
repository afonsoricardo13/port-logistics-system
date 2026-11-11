import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { OrganizationService, OrganizationDTO } from '../../services/organization.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-organizations-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './organizations-list.component.html',
  styleUrls: ['./organizations-list.component.scss']
})
export class OrganizationsListComponent implements OnInit {
  organizations: OrganizationDTO[] = [];
  filteredOrganizations: OrganizationDTO[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // Filters
  searchText = '';

  userRole = '';

  constructor(
    private organizationService: OrganizationService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.userRole = this.authService.getUserRole() || '';
    this.loadOrganizations();
  }

  loadOrganizations(): void {
    this.loading = true;
    this.error = null;

    this.organizationService.getAll().subscribe({
      next: (data) => {
        console.log('✅ Organizations loaded from backend:', data);
        console.log('First organization:', data[0]);
        this.organizations = data;
        this.filteredOrganizations = data;
        this.loading = false;
        this.applyFilters();
      },
      error: (err) => {
        console.error('Error loading organizations:', err);
        console.error('Error status:', err.status);
        console.error('Error message:', err.error);
        this.error = `Failed to load organizations: ${err.status} - ${err.error?.message || err.message || 'Unknown error'}`;
        this.loading = false;
      }
    });
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  applyFilters(): void {
    let filtered = [...this.organizations];

    // Search filter
    if (this.searchText) {
      const search = this.searchText.toLowerCase();
      filtered = filtered.filter(org =>
        org.identifier.toLowerCase().includes(search) ||
        org.legalName.toLowerCase().includes(search) ||
        org.taxNumber.toLowerCase().includes(search) ||
        org.alternativeName?.toLowerCase().includes(search)
      );
    }

    this.filteredOrganizations = filtered;
  }

  clearFilters(): void {
    this.searchText = '';
    this.applyFilters();
  }

  createNew(): void {
    this.router.navigate(['/organizations/create']);
  }

  editOrganization(org: OrganizationDTO): void {
    if (org.id) {
      this.router.navigate(['/organizations/edit', org.id]);
    }
  }

  viewRepresentatives(org: OrganizationDTO): void {
    if (org.id) {
      this.router.navigate(['/representatives'], { queryParams: { organizationId: org.id } });
    }
  }

  deactivateOrganization(org: OrganizationDTO): void {
    // Note: Deactivate functionality not yet implemented in backend
    this.error = 'Deactivate functionality is not yet available';
    setTimeout(() => this.error = null, 3000);
  }

  canManageOrganizations(): boolean {
    return this.userRole === 'PortAuthorityOfficer' || this.userRole === 'Administrator';
  }
}
