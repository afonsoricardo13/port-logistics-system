import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { VesselManagementService, VesselDTO } from '../../services/vessel-management.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-vessels-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './vessels-list.component.html',
  styleUrls: ['./vessels-list.component.scss']
})
export class VesselsListComponent implements OnInit {
  vessels: VesselDTO[] = [];
  filteredVessels: VesselDTO[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  // User information
  userRole = '';

  // Filter properties
  searchImo = '';
  searchName = '';
  searchOperator = '';

  constructor(
    private vesselService: VesselManagementService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadVessels();
  }

  private loadUserInfo(): void {
    const role = this.authService.getUserRole();
    if (role) {
      this.userRole = role;
    }
  }

  loadVessels(): void {
    this.loading = true;
    this.error = null;

    this.vesselService.getAll().subscribe({
      next: (vessels) => {
        this.vessels = vessels;
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading vessels:', err);
        this.error = 'Failed to load vessels. Please try again.';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.vessels];

    if (this.searchImo.trim()) {
      const search = this.searchImo.toLowerCase();
      filtered = filtered.filter(vessel => 
        vessel.imoNumber.toLowerCase().includes(search)
      );
    }

    if (this.searchName.trim()) {
      const search = this.searchName.toLowerCase();
      filtered = filtered.filter(vessel => 
        vessel.name.toLowerCase().includes(search)
      );
    }

    if (this.searchOperator.trim()) {
      const search = this.searchOperator.toLowerCase();
      filtered = filtered.filter(vessel => 
        vessel.operator.toLowerCase().includes(search)
      );
    }

    this.filteredVessels = filtered;
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchImo = '';
    this.searchName = '';
    this.searchOperator = '';
    this.applyFilters();
  }

  createNew(): void {
    this.router.navigate(['/vessels/new']);
  }

  editVessel(vessel: VesselDTO): void {
    this.router.navigate(['/vessels', vessel.imoNumber, 'edit']);
  }

  formatIMO(imo: string): string {
    return this.vesselService.formatIMO(imo);
  }

  canManage(): boolean {
    return this.userRole === 'PortAuthorityOfficer' || this.userRole === 'Administrator';
  }
}
