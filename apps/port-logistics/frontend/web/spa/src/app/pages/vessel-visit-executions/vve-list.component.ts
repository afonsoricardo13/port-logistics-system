import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { VesselVisitExecutionService, VesselVisitExecutionDTO } from '../../services/oem/vessel-visit-execution.service';
import { VesselVisitNotificationService, VesselVisitNotificationDTO } from '../../services/vessel-visit-notification.service';
import { VveCompleteModalComponent } from './vve-complete-modal.component';

@Component({
  selector: 'app-vve-list',
  standalone: true,
  imports: [CommonModule, VveCompleteModalComponent],
  templateUrl: './vve-list.component.html',
  styleUrl: './vve-list.component.scss'
})
export class VveListComponent implements OnInit {
  vveList: VesselVisitExecutionDTO[] = [];
  vvnMap: Map<string, VesselVisitNotificationDTO> = new Map();
  loading = false;
  error = '';
  showCompleteModal = false;
  selectedVveForCompletion: VesselVisitExecutionDTO | null = null;

  constructor(
    private vveService: VesselVisitExecutionService,
    private vvnService: VesselVisitNotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  /**
   * Load all VVEs and VVNs
   */
  private loadData(): void {
    this.loading = true;
    
    // Load VVNs first to build the map
    this.vvnService.getAll().subscribe({
      next: (vvns) => {
        vvns.forEach(vvn => {
          if (vvn.id) this.vvnMap.set(vvn.id.toString(), vvn);
        });
        // Then load VVEs
        this.loadVVEs();
      },
      error: (err) => {
        console.error('Error loading VVNs:', err);
        // Continue to load VVEs even if VVNs fail
        this.loadVVEs();
      }
    });
  }

  private loadVVEs(): void {
    this.vveService.getAll().subscribe({
      next: (vves: VesselVisitExecutionDTO[]) => {
        this.vveList = vves;
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Error loading VVEs:', err);
        this.error = 'Failed to load vessel visit executions.';
        this.loading = false;
      }
    });
  }

  /**
   * Get planned dock for a VVE
   */
  getPlannedDock(vvnId: string): string {
    const vvn = this.vvnMap.get(vvnId);
    return vvn?.assignedDockCode || 'N/A';
  }

  /**
   * Navigate to create VVE page
   */
  createNew(): void {
    this.router.navigate(['/operations/executions/create']);
  }

  /**
   * US 4.1.10: Navigate to search page with metrics
   */
  navigateToSearch(): void {
    this.router.navigate(['/operations/executions/search']);
  }

  /**
   * Navigate to update berthing page
   */
  updateBerthing(id: string): void {
    this.router.navigate(['/operations/executions', id, 'berthing']);
  }

  /**
   * Navigate to track execution page (US 4.1.9)
   */
  trackExecution(id: string): void {
    this.router.navigate(['/operations/executions', id, 'track']);
  }

  /**
   * US 4.1.11: Open complete modal
   */
  openCompleteModal(vve: VesselVisitExecutionDTO): void {
    this.selectedVveForCompletion = vve;
    this.showCompleteModal = true;
  }

  /**
   * US 4.1.11: Handle completion success
   */
  onVveCompleted(): void {
    this.showCompleteModal = false;
    this.selectedVveForCompletion = null;
    this.loadData(); // Reload data to reflect changes
  }

  /**
   * US 4.1.11: Handle modal cancel
   */
  onCompleteCancelled(): void {
    this.showCompleteModal = false;
    this.selectedVveForCompletion = null;
  }

  /**
   * Check if VVE can be completed
   */
  canComplete(vve: VesselVisitExecutionDTO): boolean {
    return vve.status === 'InProgress' && !!vve.actualBerthTime;
  }

  /**
   * Format date for display
   */
  formatDate(date: string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString();
  }

  /**
   * Get status badge class
   */
  getStatusClass(status: string): string {
    return status === 'InProgress' ? 'status-in-progress' : 'status-completed';
  }
}
