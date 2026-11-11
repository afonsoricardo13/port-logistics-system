import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { VesselTypeManagementService, VesselTypeDTO } from '../../services/vessel-type-management.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-vessel-types-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './vessel-types-list.component.html',
  styleUrls: ['./vessel-types-list.component.scss']
})
export class VesselTypesListComponent implements OnInit {
  vesselTypes: VesselTypeDTO[] = [];
  filteredVesselTypes: VesselTypeDTO[] = [];
  isLoading = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // User information
  userRole = '';

  // Search filters
  searchName = '';
  searchDescription = '';

  constructor(
    private vesselTypeService: VesselTypeManagementService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadVesselTypes();
  }

  private loadUserInfo(): void {
    const role = this.authService.getUserRole();
    if (role) {
      this.userRole = role;
    }
  }

  loadVesselTypes(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.vesselTypeService.getAll().subscribe({
      next: (data) => {
        this.vesselTypes = data;
        this.applyFilters();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading vessel types:', error);
        this.errorMessage = 'Failed to load vessel types. Please try again.';
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.vesselTypes];

    if (this.searchName.trim()) {
      const search = this.searchName.toLowerCase();
      filtered = filtered.filter(vt => 
        vt.name.toLowerCase().includes(search)
      );
    }

    if (this.searchDescription.trim()) {
      const search = this.searchDescription.toLowerCase();
      filtered = filtered.filter(vt => 
        vt.description.toLowerCase().includes(search)
      );
    }

    this.filteredVesselTypes = filtered;
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchName = '';
    this.searchDescription = '';
    this.applyFilters();
  }

  createNew(): void {
    this.router.navigate(['/vessel-types/new']);
  }

  editVesselType(vt: VesselTypeDTO): void {
    this.router.navigate(['/vessel-types', vt.name, 'edit']);
  }

  canManage(): boolean {
    return this.userRole === 'PortAuthorityOfficer' || this.userRole === 'Administrator';
  }
}
