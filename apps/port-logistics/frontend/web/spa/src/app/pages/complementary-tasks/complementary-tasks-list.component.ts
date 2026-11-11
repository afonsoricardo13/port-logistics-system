import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ComplementaryTasksService, ComplementaryTaskDTO, ComplementaryTaskFilterParams } from '../../services/complementary-tasks.service';

/**
 * US 4.1.15 - Complementary Tasks Management
 * List and manage non-cargo activities performed during vessel visits
 */
@Component({
  selector: 'app-complementary-tasks-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './complementary-tasks-list.component.html',
  styleUrls: ['./complementary-tasks-list.component.scss']
})
export class ComplementaryTasksListComponent implements OnInit {
  // Filter parameters
  statusFilter: string = '';
  vveIdFilter: string = '';
  startDateFilter: string = '';
  endDateFilter: string = '';
  ongoingOnlyFilter: boolean = false;
  impactTypeFilter: string = '';

  // State
  tasks: ComplementaryTaskDTO[] = [];
  filteredTasks: ComplementaryTaskDTO[] = [];
  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Pagination
  currentPage: number = 1;
  pageSize: number = 10;

  // Options
  statusOptions = ['', 'Ongoing', 'Completed'];
  impactTypeOptions = ['', 'Parallel', 'Blocking'];

  constructor(
    private tasksService: ComplementaryTasksService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadTasks();
  }

  /**
   * Load tasks with current filters
   */
  loadTasks(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const filters: ComplementaryTaskFilterParams = {};
    
    if (this.statusFilter) filters.status = this.statusFilter;
    if (this.vveIdFilter) filters.vveId = this.vveIdFilter;
    if (this.startDateFilter) filters.startDate = this.startDateFilter;
    if (this.endDateFilter) filters.endDate = this.endDateFilter;
    if (this.ongoingOnlyFilter) filters.ongoingOnly = true;
    if (this.impactTypeFilter) filters.impactType = this.impactTypeFilter;

    this.tasksService.getAll(filters).subscribe({
      next: (data) => {
        this.tasks = data;
        this.filteredTasks = data;
        this.isLoading = false;
        this.currentPage = 1;
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to load complementary tasks';
        this.isLoading = false;
      }
    });
  }

  /**
   * Apply filters
   */
  applyFilters(): void {
    this.loadTasks();
  }

  /**
   * Reset all filters
   */
  resetFilters(): void {
    this.statusFilter = '';
    this.vveIdFilter = '';
    this.startDateFilter = '';
    this.endDateFilter = '';
    this.ongoingOnlyFilter = false;
    this.impactTypeFilter = '';
    this.loadTasks();
  }

  /**
   * Navigate to create task form
   */
  createTask(): void {
    this.router.navigate(['/operations/complementary-tasks/create']);
  }

  /**
   * Navigate to edit task form
   */
  editTask(id: string): void {
    this.router.navigate(['/operations/complementary-tasks/edit', id]);
  }

  /**
   * Mark task as completed
   */
  completeTask(task: ComplementaryTaskDTO): void {
    if (!confirm(`Mark this task as completed?\n\n${task.categoryName} - ${task.responsibleTeam}`)) {
      return;
    }

    this.tasksService.complete(task.id).subscribe({
      next: () => {
        this.successMessage = 'Task marked as completed';
        this.loadTasks();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to complete task';
        setTimeout(() => this.errorMessage = '', 5000);
      }
    });
  }

  /**
   * Delete a task
   */
  deleteTask(task: ComplementaryTaskDTO): void {
    if (!confirm(`Delete this task?\n\n${task.categoryName} - ${task.responsibleTeam}\n\nThis action cannot be undone.`)) {
      return;
    }

    this.tasksService.delete(task.id).subscribe({
      next: () => {
        this.successMessage = 'Task deleted successfully';
        this.loadTasks();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to delete task';
        setTimeout(() => this.errorMessage = '', 5000);
      }
    });
  }

  /**
   * Get paginated tasks
   */
  get paginatedTasks(): ComplementaryTaskDTO[] {
    const start = (this.currentPage - 1) * this.pageSize;
    const end = start + this.pageSize;
    return this.filteredTasks.slice(start, end);
  }

  /**
   * Get total pages
   */
  get totalPages(): number {
    return Math.ceil(this.filteredTasks.length / this.pageSize);
  }

  /**
   * Go to page
   */
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  /**
   * Get CSS class for status badge
   */
  getStatusClass(status: string): string {
    return status === 'Ongoing' ? 'badge-warning' : 'badge-success';
  }

  /**
   * Get CSS class for impact type badge
   */
  getImpactClass(impactType: string): string {
    return impactType === 'Blocking' ? 'badge-danger' : 'badge-info';
  }

  /**
   * Format date for display
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleString();
  }

  /**
   * Format duration
   */
  formatDuration(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`;
  }
}
