import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { 
  VesselVisitExecutionService, 
  VesselVisitExecutionSearchResultDTO 
} from '../../services/oem/vessel-visit-execution.service';

@Component({
  selector: 'app-vve-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './vve-search.component.html',
  styleUrl: './vve-search.component.scss'
})
export class VveSearchComponent implements OnInit {
  // Filter inputs
  startDate: string = '';
  endDate: string = '';
  vesselImo: string = '';
  status: string = '';

  // Results and state
  searchResults: VesselVisitExecutionSearchResultDTO[] = [];
  loading = false;
  error = '';
  searched = false;

  // Status options for dropdown
  statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'InProgress', label: 'In Progress' },
    { value: 'Completed', label: 'Completed' }
  ];

  constructor(private vveService: VesselVisitExecutionService) {}

  ngOnInit(): void {
    // Set default date range: last 30 days to today
    const today = new Date();
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(today.getDate() - 30);

    this.endDate = this.formatDate(today);
    this.startDate = this.formatDate(thirtyDaysAgo);

    // Perform initial search
    this.search();
  }

  /**
   * US 4.1.10: Perform search with filters
   */
  search(): void {
    this.loading = true;
    this.error = '';
    this.searched = true;

    const startDateParam = this.startDate || undefined;
    const endDateParam = this.endDate || undefined;
    const vesselImoParam = this.vesselImo.trim() || undefined;
    const statusParam = this.status || undefined;

    this.vveService.searchWithMetrics(
      startDateParam,
      endDateParam,
      vesselImoParam,
      statusParam
    ).subscribe({
      next: (results) => {
        this.searchResults = results;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error searching VVEs:', err);
        this.error = 'Failed to search vessel visit executions. Please try again.';
        this.loading = false;
      }
    });
  }

  /**
   * Reset filters and search
   */
  resetFilters(): void {
    const today = new Date();
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(today.getDate() - 30);

    this.endDate = this.formatDate(today);
    this.startDate = this.formatDate(thirtyDaysAgo);
    this.vesselImo = '';
    this.status = '';
    this.search();
  }

  /**
   * Format date to YYYY-MM-DD for input[type="date"]
   */
  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  /**
   * Format datetime for display
   */
  formatDateTime(dateStr?: string): string {
    if (!dateStr) return 'N/A';
    const date = new Date(dateStr);
    return date.toLocaleString('en-GB', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Format number to 2 decimal places
   */
  formatNumber(num?: number): string {
    if (num === undefined || num === null) return 'N/A';
    return num.toFixed(2);
  }

  /**
   * Get status badge class
   */
  getStatusClass(status: string): string {
    switch (status) {
      case 'Completed':
        return 'badge-success';
      case 'InProgress':
        return 'badge-warning';
      default:
        return 'badge-secondary';
    }
  }

  /**
   * Get delay class (positive = late, negative = early)
   */
  getDelayClass(delay?: number): string {
    if (delay === undefined || delay === null) return '';
    if (delay > 0) return 'text-danger'; // Late
    if (delay < 0) return 'text-success'; // Early
    return 'text-muted'; // On time
  }

  /**
   * Format delay with sign
   */
  formatDelay(delay?: number): string {
    if (delay === undefined || delay === null) return 'N/A';
    const sign = delay > 0 ? '+' : '';
    return `${sign}${delay.toFixed(2)}h`;
  }

  /**
   * Count results by status
   */
  countByStatus(status: string): number {
    return this.searchResults.filter(r => r.status === status).length;
  }
}
