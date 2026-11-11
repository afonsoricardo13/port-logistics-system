import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { OperationPlanSearchService, OperationPlanDetail } from '../../services/operation-plan-search.service';

/**
 * US 4.1.3 - Search and List Operation Plans
 */
@Component({
  selector: 'app-operation-plans-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './operation-plans-list.component.html',
  styleUrls: ['./operation-plans-list.component.scss']
})
export class OperationPlansListComponent implements OnInit {
  vvnId: string = '';
  singleDate: string = '';
  startDate: string = '';
  endDate: string = '';
  searchMode: 'single' | 'range' = 'single';
  sortBy: 'startTime' | 'endTime' | 'duration' | 'vesselId' | 'delay' = 'startTime';
  sortOrder: 'asc' | 'desc' = 'asc';
  isLoading: boolean = false;
  errorMessage: string = '';
  plans: OperationPlanDetail[] = [];
  currentPage: number = 1;
  pageSize: number = 10;

  constructor(
    private searchService: OperationPlanSearchService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const today = new Date();
    this.singleDate = this.formatDateForInput(today);
    this.search();
  }

  search(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.currentPage = 1;

    const params: any = { sortBy: this.sortBy, sortOrder: this.sortOrder };
    if (this.vvnId) params.vvnId = this.vvnId;
    if (this.searchMode === 'single' && this.singleDate) params.date = this.singleDate;
    else if (this.searchMode === 'range') {
      if (this.startDate) params.startDate = this.startDate;
      if (this.endDate) params.endDate = this.endDate;
    }

    this.searchService.searchOperationPlans(params).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.plans = response;
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Failed to load operation plans';
      }
    });
  }

  reset(): void {
    this.vvnId = '';
    this.singleDate = this.formatDateForInput(new Date());
    this.startDate = '';
    this.endDate = '';
    this.searchMode = 'single';
    this.sortBy = 'startTime';
    this.sortOrder = 'asc';
    this.search();
  }

  sort(field: 'startTime' | 'endTime' | 'duration' | 'vesselId' | 'delay'): void {
    if (this.sortBy === field) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = field;
      this.sortOrder = 'asc';
    }
    this.search();
  }

  get totalPages(): number {
    return Math.ceil(this.plans.length / this.pageSize);
  }

  get paginatedPlans(): OperationPlanDetail[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.plans.slice(start, start + this.pageSize);
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
  }

  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
  }

  formatTime(date: string | null): string {
    if (!date) return '-';
    return new Date(date).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-GB');
  }

  formatDuration(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}`;
  }

  formatDelayHours(delayValue: number): string {
    // Delay from Prolog is already in hours
    // Show more precision for small values
    if (delayValue < 1) {
      return `${delayValue.toFixed(2)}h`;
    }
    return `${delayValue.toFixed(2)}h`;
  }

  getCrane(resources: any[]): string {
    const crane = resources.find(r => r.type === 'crane');
    return crane?.id || '-';
  }

  getStaff(resources: any[]): string {
    const staff = resources.find(r => r.type === 'staff');
    return staff?.id || '-';
  }

  getStorage(resources: any[]): string {
    const storage = resources.find(r => r.type === 'storage');
    return storage?.id || '-';
  }

  editPlan(planId: string): void {
    console.log('Editing plan with ID:', planId);
    this.router.navigate(['/operations/operation-plans/update', planId]);
  }

  private formatDateForInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
