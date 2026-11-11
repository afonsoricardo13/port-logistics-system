import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { IncidentsService, IncidentDTO, IncidentFilterParams } from '../../services/incidents.service';

/**
 * US 4.1.13 - Incidents Management
 * List and filter incidents affecting port operations
 */
@Component({
  selector: 'app-incidents-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './incidents-list.component.html',
  styleUrls: ['./incidents-list.component.scss']
})
export class IncidentsListComponent implements OnInit {
  // Filter parameters
  statusFilter: string = '';
  severityFilter: string = '';
  startDateFilter: string = '';
  endDateFilter: string = '';
  activeOnlyFilter: boolean = false;
  vesselVisitIdFilter: string = '';

  // State
  incidents: IncidentDTO[] = [];
  filteredIncidents: IncidentDTO[] = [];
  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Pagination
  currentPage: number = 1;
  pageSize: number = 10;

  // Options
  statusOptions = ['', 'Active', 'Resolved'];
  severityOptions = ['', 'Minor', 'Major', 'Critical'];

  constructor(
    private incidentsService: IncidentsService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadIncidents();
  }

  /**
   * Load incidents with current filters
   */
  loadIncidents(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const filters: IncidentFilterParams = {};
    
    if (this.statusFilter) filters.status = this.statusFilter;
    if (this.severityFilter) filters.severity = this.severityFilter;
    if (this.startDateFilter) filters.startDate = this.startDateFilter;
    if (this.endDateFilter) filters.endDate = this.endDateFilter;
    if (this.activeOnlyFilter) filters.activeOnly = true;
    if (this.vesselVisitIdFilter) filters.vesselVisitId = this.vesselVisitIdFilter;

    this.incidentsService.getAll(filters).subscribe({
      next: (data) => {
        this.incidents = data;
        this.filteredIncidents = data;
        this.isLoading = false;
        this.currentPage = 1;
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to load incidents';
        this.isLoading = false;
      }
    });
  }

  /**
   * Apply filters
   */
  applyFilters(): void {
    this.loadIncidents();
  }

  /**
   * Reset all filters
   */
  resetFilters(): void {
    this.statusFilter = '';
    this.severityFilter = '';
    this.startDateFilter = '';
    this.endDateFilter = '';
    this.activeOnlyFilter = false;
    this.vesselVisitIdFilter = '';
    this.loadIncidents();
  }

  /**
   * Navigate to create incident form
   */
  createIncident(): void {
    this.router.navigate(['/operations/incidents/create']);
  }

  /**
   * Navigate to edit incident form
   */
  editIncident(id: string): void {
    this.router.navigate(['/operations/incidents/edit', id]);
  }

  /**
   * Mark incident as resolved
   */
  resolveIncident(incident: IncidentDTO): void {
    if (!confirm(`Mark incident as resolved?\n\n${incident.description}`)) {
      return;
    }

    this.incidentsService.resolve(incident.id).subscribe({
      next: () => {
        this.successMessage = 'Incident marked as resolved';
        this.loadIncidents();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to resolve incident';
        setTimeout(() => this.errorMessage = '', 5000);
      }
    });
  }

  /**
   * Delete an incident
   */
  deleteIncident(incident: IncidentDTO): void {
    if (!confirm(`Delete this incident?\n\n${incident.description}\n\nThis action cannot be undone.`)) {
      return;
    }

    this.incidentsService.delete(incident.id).subscribe({
      next: () => {
        this.successMessage = 'Incident deleted successfully';
        this.loadIncidents();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to delete incident';
        setTimeout(() => this.errorMessage = '', 5000);
      }
    });
  }

  /**
   * Get severity badge class
   */
  getSeverityClass(severity: string): string {
    switch (severity) {
      case 'Critical': return 'severity-critical';
      case 'Major': return 'severity-major';
      case 'Minor': return 'severity-minor';
      default: return '';
    }
  }

  /**
   * Get status badge class
   */
  getStatusClass(status: string): string {
    return status === 'Active' ? 'status-active' : 'status-resolved';
  }

  /**
   * Format date for display
   */
  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString('en-GB', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Format duration
   */
  formatDuration(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return `${hours}h ${mins}m`;
  }

  /**
   * Get paginated incidents
   */
  get paginatedIncidents(): IncidentDTO[] {
    const start = (this.currentPage - 1) * this.pageSize;
    const end = start + this.pageSize;
    return this.filteredIncidents.slice(start, end);
  }

  /**
   * Get total pages
   */
  get totalPages(): number {
    return Math.ceil(this.filteredIncidents.length / this.pageSize);
  }

  /**
   * Navigate to page
   */
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  /**
   * Get page numbers for pagination
   */
  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }
}
