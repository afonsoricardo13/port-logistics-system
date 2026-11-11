// US 2.2.6: Manage Representatives (view, create, update, deactivate)
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { RepresentativeService, RepresentativeDTO } from '../../services/representative.service';
import { OrganizationService, OrganizationDTO } from '../../services/organization.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-representatives-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './representatives-list.component.html',
  styleUrls: ['./representatives-list.component.scss']
})
export class RepresentativesListComponent implements OnInit {
  representatives: RepresentativeDTO[] = [];
  filteredRepresentatives: RepresentativeDTO[] = [];
  organizations: OrganizationDTO[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // Filters
  searchText = '';
  selectedOrganizationId: number | null = null;

  userRole = '';

  constructor(
    private representativeService: RepresentativeService,
    private organizationService: OrganizationService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.userRole = this.authService.getUserRole() || '';
    
    // Check if filtering by organization from query params
    this.route.queryParams.subscribe(params => {
      if (params['organizationId']) {
        this.selectedOrganizationId = +params['organizationId'];
      }
    });

    this.loadOrganizations();
    this.loadRepresentatives();
  }

  loadOrganizations(): void {
    this.organizationService.getAll().subscribe({
      next: (data) => {
        this.organizations = data;
      },
      error: (err) => {
        console.error('Error loading organizations:', err);
      }
    });
  }

  loadRepresentatives(): void {
    this.loading = true;
    this.error = null;

    const loadObservable = this.selectedOrganizationId
      ? this.representativeService.getByOrganization(this.selectedOrganizationId)
      : this.representativeService.getAll();

    loadObservable.subscribe({
      next: (data) => {
        console.log('✅ Representatives loaded from backend:', data);
        console.log('First representative:', data[0]);
        this.representatives = data;
        this.filteredRepresentatives = data;
        this.loading = false;
        this.applyFilters();
      },
      error: (err) => {
        console.error('Error loading representatives:', err);
        console.error('Error status:', err.status);
        console.error('Error message:', err.error);
        this.error = `Failed to load representatives: ${err.status} - ${err.error?.message || err.message || 'Unknown error'}`;
        this.loading = false;
      }
    });
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onOrganizationFilterChange(): void {
    this.loadRepresentatives();
  }

  applyFilters(): void {
    let filtered = [...this.representatives];

    // Search filter
    if (this.searchText) {
      const search = this.searchText.toLowerCase();
      filtered = filtered.filter(rep =>
        rep.name.toLowerCase().includes(search) ||
        rep.email.toLowerCase().includes(search) ||
        rep.citizenId.toLowerCase().includes(search)
      );
    }

    this.filteredRepresentatives = filtered;
  }

  clearFilters(): void {
    this.searchText = '';
    this.selectedOrganizationId = null;
    this.loadRepresentatives();
  }

  createNew(): void {
    const queryParams = this.selectedOrganizationId
      ? { organizationId: this.selectedOrganizationId }
      : {};
    this.router.navigate(['/representatives/create'], { queryParams });
  }

  editRepresentative(rep: RepresentativeDTO): void {
    if (rep.id) {
      this.router.navigate(['/representatives/edit', rep.id]);
    }
  }

  deactivateRepresentative(rep: RepresentativeDTO): void {
    if (!rep.id) return;

    if (!confirm(`Are you sure you want to deactivate representative "${rep.name}"?`)) {
      return;
    }

    this.representativeService.deactivate(rep.id).subscribe({
      next: () => {
          this.successMessage = `Representative ${rep.name} deactivated successfully`;
        setTimeout(() => this.successMessage = null, 5000);
        this.loadRepresentatives();
      },
      error: (err) => {
        console.error('Error deactivating representative:', err);
        if (err.status === 409 && err.error?.message) {
          this.error = err.error.message;
        } else {
          this.error = 'Failed to deactivate representative';
        }
      }
    });
  }

  reactivateRepresentative(rep: RepresentativeDTO): void {
    if (!rep.id) return;

    if (!confirm(`Are you sure you want to reactivate representative "${rep.name}"?`)) {
      return;
    }

    this.representativeService.reactivate(rep.id).subscribe({
      next: () => {
          this.successMessage = `Representative ${rep.name} reactivated successfully`;
        setTimeout(() => this.successMessage = null, 5000);
        this.loadRepresentatives();
      },
      error: (err) => {
        console.error('Error reactivating representative:', err);
        this.error = 'Failed to reactivate representative';
      }
    });
  }

  getOrganizationName(organizationId: number): string {
    const org = this.organizations.find(o => o.id === organizationId);
    return org ? (org.legalName || org.identifier) : 'Unknown';
  }

  canManageRepresentatives(): boolean {
    return this.userRole === 'PortAuthorityOfficer' || this.userRole === 'Administrator';
  }
}
