import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DockManagementService, DockDTO } from '../../services/dock-management.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-docks-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './docks-list.component.html',
  styleUrls: ['./docks-list.component.scss']
})
export class DocksListComponent implements OnInit {
  docks: DockDTO[] = [];
  filteredDocks: DockDTO[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // User information
  userRole = '';

  // Filter properties
  searchText = '';
  selectedLocation = '';
  selectedVesselType = '';

  constructor(
    private dockService: DockManagementService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadDocks();
  }

  private loadUserInfo(): void {
    const role = this.authService.getUserRole();
    if (role) {
      this.userRole = role;
    }
  }

  loadDocks(): void {
    this.loading = true;
    this.error = null;

    this.dockService.getAll().subscribe({
      next: (docks) => {
        this.docks = docks;
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading docks:', err);
        this.error = 'Failed to load docks. Please try again.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.docks];

    // Apply search text
    if (this.searchText.trim()) {
      const search = this.searchText.toLowerCase();
      filtered = filtered.filter(dock => 
        dock.code.toLowerCase().includes(search) ||
        dock.nameOrNumber.toLowerCase().includes(search)
      );
    }

    // Filter by location
    if (this.selectedLocation.trim()) {
      const location = this.selectedLocation.toLowerCase();
      filtered = filtered.filter(dock => 
        dock.location.toLowerCase().includes(location)
      );
    }

    // Filter by vessel type
    if (this.selectedVesselType.trim()) {
      const vesselType = this.selectedVesselType.toLowerCase();
      filtered = filtered.filter(dock => 
        dock.allowedVesselTypes.some(vt => vt.toLowerCase().includes(vesselType))
      );
    }

    this.filteredDocks = filtered;
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchText = '';
    this.selectedLocation = '';
    this.selectedVesselType = '';
    this.applyFilters();
  }

  createNew(): void {
    this.router.navigate(['/docks/new']);
  }

  editDock(dock: DockDTO): void {
    this.router.navigate(['/docks', dock.code, 'edit']);
  }

  canManage(): boolean {
    return this.userRole === 'PortAuthorityOfficer' || this.userRole === 'Administrator';
  }
}
