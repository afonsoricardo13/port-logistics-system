import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { StorageAreaManagementService, StorageAreaDTO } from '../../services/storage-area-management.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-storage-areas-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './storage-areas-list.component.html',
  styleUrls: ['./storage-areas-list.component.scss']
})
export class StorageAreasListComponent implements OnInit {
  storageAreas: StorageAreaDTO[] = [];
  filteredAreas: StorageAreaDTO[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // User information
  userRole = '';

  // Filter properties
  searchText = '';
  selectedType = '';
  selectedLocation = '';

  constructor(
    private service: StorageAreaManagementService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadStorageAreas();
  }

  private loadUserInfo(): void {
    const role = this.authService.getUserRole();
    if (role) {
      this.userRole = role;
    }
  }

  loadStorageAreas(): void {
    this.loading = true;
    this.error = null;

    this.service.getAll().subscribe({
      next: (areas) => {
        this.storageAreas = areas;
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading storage areas:', err);
        this.error = 'Failed to load storage areas. Please try again.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.storageAreas];

    if (this.searchText.trim()) {
      const search = this.searchText.toLowerCase();
      filtered = filtered.filter(area => 
        area.identifier.toLowerCase().includes(search)
      );
    }

    if (this.selectedType) {
      filtered = filtered.filter(area => area.type === this.selectedType);
    }

    if (this.selectedLocation.trim()) {
      const location = this.selectedLocation.toLowerCase();
      filtered = filtered.filter(area => 
        area.location.toLowerCase().includes(location)
      );
    }

    this.filteredAreas = filtered;
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchText = '';
    this.selectedType = '';
    this.selectedLocation = '';
    this.applyFilters();
  }

  createNew(): void {
    this.router.navigate(['/storage-areas/new']);
  }

  editArea(area: StorageAreaDTO): void {
    this.router.navigate(['/storage-areas', area.identifier, 'edit']);
  }

  getOccupancyPercentage(area: StorageAreaDTO): number {
    return this.service.getOccupancyPercentage(area);
  }

  getOccupancyClass(percentage: number): string {
    if (percentage >= 90) return 'fill-danger';
    if (percentage >= 75) return 'fill-warning';
    return 'fill-success';
  }

  getTypeClass(type: string): string {
    return type === 'Yard' ? 'type-yard' : 'type-warehouse';
  }

  getServedDocksDisplay(area: StorageAreaDTO): string {
    if (this.service.servesAllDocks(area)) {
      return 'All Docks';
    }
    return area.servedDocks.map(d => d.dockCode).join(', ');
  }

  canManage(): boolean {
    return this.userRole === 'PortAuthorityOfficer' || this.userRole === 'Administrator';
  }
}
