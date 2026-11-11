import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ComplementaryTasksService, CreateComplementaryTaskDTO, UpdateComplementaryTaskDTO, ComplementaryTaskDTO } from '../../services/complementary-tasks.service';
import { ComplementaryTaskCategoryService, ComplementaryTaskCategoryDTO } from '../../services/complementary-task-category.service';
import { VesselVisitExecutionService, VesselVisitExecutionDTO } from '../../services/oem/vessel-visit-execution.service';

@Component({
  selector: 'app-complementary-tasks-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './complementary-tasks-form.component.html',
  styleUrls: ['./complementary-tasks-form.component.scss']
})
export class ComplementaryTasksFormComponent implements OnInit {
  taskId: string | null = null;
  isEditMode: boolean = false;
  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Form data
  categoryCode: string = '';
  vveId: string = '';
  responsibleTeam: string = '';
  startTime: string = '';
  endTime: string = '';
  impactType: string = 'Parallel';
  status: string = 'Ongoing';
  notes: string = '';

  // Options
  categories: ComplementaryTaskCategoryDTO[] = [];
  vves: VesselVisitExecutionDTO[] = [];
  impactTypes = ['Parallel', 'Blocking'];
  statuses = ['Ongoing', 'Completed'];

  constructor(
    private tasksService: ComplementaryTasksService,
    private categoriesService: ComplementaryTaskCategoryService,
    private vvesService: VesselVisitExecutionService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.taskId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.taskId;

    this.loadCategories();
    this.loadVVEs();

    if (this.isEditMode && this.taskId) {
      this.loadTask(this.taskId);
    } else {
      // Set default start time to now
      this.startTime = new Date().toISOString().slice(0, 16);
    }
  }

  loadCategories(): void {
    this.categoriesService.getAll().subscribe({
      next: (data: ComplementaryTaskCategoryDTO[]) => this.categories = data,
      error: (error: any) => console.error('Failed to load categories', error)
    });
  }

  loadVVEs(): void {
    // Load all VVEs - we'll filter in-progress ones on the client side
    this.vvesService.getAll().subscribe({
      next: (data: VesselVisitExecutionDTO[]) => {
        // Filter only in-progress VVEs
        this.vves = data.filter(vve => vve.status === 'InProgress');
      },
      error: (error: any) => console.error('Failed to load VVEs', error)
    });
  }

  loadTask(id: string): void {
    this.isLoading = true;
    this.tasksService.getById(id).subscribe({
      next: (task) => {
        this.categoryCode = task.categoryCode;
        this.vveId = task.vveId;
        this.responsibleTeam = task.responsibleTeam;
        this.startTime = new Date(task.startTime).toISOString().slice(0, 16);
        this.endTime = task.endTime ? new Date(task.endTime).toISOString().slice(0, 16) : '';
        this.impactType = task.impactType;
        this.status = task.status;
        this.notes = task.notes || '';
        this.isLoading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load task';
        this.isLoading = false;
      }
    });
  }

  save(): void {
    if (!this.validate()) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    if (this.isEditMode && this.taskId) {
      this.updateTask();
    } else {
      this.createTask();
    }
  }

  createTask(): void {
    const dto: CreateComplementaryTaskDTO = {
      categoryCode: this.categoryCode,
      vveId: this.vveId,
      responsibleTeam: this.responsibleTeam,
      startTime: new Date(this.startTime).toISOString(),
      impactType: this.impactType,
      notes: this.notes || undefined
    };

    this.tasksService.create(dto).subscribe({
      next: () => {
        this.successMessage = 'Task created successfully';
        setTimeout(() => this.router.navigate(['/operations/complementary-tasks']), 1500);
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to create task';
        this.isLoading = false;
      }
    });
  }

  updateTask(): void {
    const dto: UpdateComplementaryTaskDTO = {
      categoryCode: this.categoryCode,
      responsibleTeam: this.responsibleTeam,
      startTime: new Date(this.startTime).toISOString(),
      endTime: this.endTime ? new Date(this.endTime).toISOString() : undefined,
      status: this.status,
      impactType: this.impactType,
      notes: this.notes || undefined
    };

    this.tasksService.update(this.taskId!, dto).subscribe({
      next: () => {
        this.successMessage = 'Task updated successfully';
        setTimeout(() => this.router.navigate(['/operations/complementary-tasks']), 1500);
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to update task';
        this.isLoading = false;
      }
    });
  }

  validate(): boolean {
    if (!this.categoryCode) {
      this.errorMessage = 'Please select a category';
      return false;
    }
    if (!this.vveId) {
      this.errorMessage = 'Please select a vessel visit execution';
      return false;
    }
    if (!this.responsibleTeam) {
      this.errorMessage = 'Please enter responsible team';
      return false;
    }
    if (!this.startTime) {
      this.errorMessage = 'Please select start time';
      return false;
    }
    if (this.endTime && new Date(this.endTime) < new Date(this.startTime)) {
      this.errorMessage = 'End time must be after start time';
      return false;
    }
    return true;
  }

  cancel(): void {
    this.router.navigate(['/operations/complementary-tasks']);
  }
}
