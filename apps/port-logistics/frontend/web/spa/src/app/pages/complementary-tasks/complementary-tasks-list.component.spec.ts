import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ComplementaryTasksListComponent } from './complementary-tasks-list.component';
import { ComplementaryTasksService, ComplementaryTaskDTO } from '../../services/complementary-tasks.service';

describe('ComplementaryTasksListComponent', () => {
  let component: ComplementaryTasksListComponent;
  let fixture: ComponentFixture<ComplementaryTasksListComponent>;
  let mockService: jasmine.SpyObj<ComplementaryTasksService>;
  let mockRouter: jasmine.SpyObj<Router>;

  const mockTask: ComplementaryTaskDTO = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    categoryCode: 'CTC001',
    categoryName: 'Inspection',
    vveId: 'VVE-001',
    vesselName: 'Test Vessel',
    responsibleTeam: 'Team A',
    startTime: '2025-01-15T10:00:00Z',
    status: 'Ongoing',
    impactType: 'Parallel',
    createdAt: '2025-01-15T09:00:00Z',
    isOngoing: true
  };

  beforeEach(async () => {
    mockService = jasmine.createSpyObj('ComplementaryTasksService', [
      'getAll', 'getOngoing', 'getById', 'getByVveId', 'create', 'update', 'complete', 'delete'
    ]);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [ComplementaryTasksListComponent],
      providers: [
        { provide: ComplementaryTasksService, useValue: mockService },
        { provide: Router, useValue: mockRouter }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ComplementaryTasksListComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should load tasks on init', () => {
      mockService.getAll.and.returnValue(of([mockTask]));

      component.ngOnInit();

      expect(mockService.getAll).toHaveBeenCalled();
      expect(component.tasks.length).toBe(1);
      expect(component.isLoading).toBe(false);
    });
  });

  describe('loadTasks', () => {
    it('should load all tasks without filters', () => {
      mockService.getAll.and.returnValue(of([mockTask]));

      component.loadTasks();

      expect(mockService.getAll).toHaveBeenCalledWith({});
      expect(component.tasks).toEqual([mockTask]);
      expect(component.isLoading).toBe(false);
    });

    it('should load tasks with status filter', () => {
      component.statusFilter = 'Ongoing';
      mockService.getAll.and.returnValue(of([mockTask]));

      component.loadTasks();

      expect(mockService.getAll).toHaveBeenCalledWith({ status: 'Ongoing' });
    });

    it('should load tasks with vveId filter', () => {
      component.vveIdFilter = 'VVE-001';
      mockService.getAll.and.returnValue(of([mockTask]));

      component.loadTasks();

      expect(mockService.getAll).toHaveBeenCalledWith({ vveId: 'VVE-001' });
    });

    it('should load tasks with date range filter', () => {
      component.startDateFilter = '2025-01-01';
      component.endDateFilter = '2025-01-31';
      mockService.getAll.and.returnValue(of([mockTask]));

      component.loadTasks();

      expect(mockService.getAll).toHaveBeenCalledWith({
        startDate: '2025-01-01',
        endDate: '2025-01-31'
      });
    });

    it('should load tasks with ongoingOnly filter', () => {
      component.ongoingOnlyFilter = true;
      mockService.getAll.and.returnValue(of([mockTask]));

      component.loadTasks();

      expect(mockService.getAll).toHaveBeenCalledWith({ ongoingOnly: true });
    });

    it('should handle error when loading tasks', () => {
      const error = { error: { message: 'Failed to load' } };
      mockService.getAll.and.returnValue(throwError(() => error));

      component.loadTasks();

      expect(component.errorMessage).toBe('Failed to load');
      expect(component.isLoading).toBe(false);
    });
  });

  describe('applyFilters', () => {
    it('should reload tasks when filters are applied', () => {
      spyOn(component, 'loadTasks');

      component.applyFilters();

      expect(component.loadTasks).toHaveBeenCalled();
    });
  });

  describe('resetFilters', () => {
    it('should clear all filters and reload tasks', () => {
      component.statusFilter = 'Ongoing';
      component.vveIdFilter = 'VVE-001';
      component.ongoingOnlyFilter = true;
      mockService.getAll.and.returnValue(of([mockTask]));

      component.resetFilters();

      expect(component.statusFilter).toBe('');
      expect(component.vveIdFilter).toBe('');
      expect(component.ongoingOnlyFilter).toBe(false);
      expect(mockService.getAll).toHaveBeenCalled();
    });
  });

  describe('createTask', () => {
    it('should navigate to create form', () => {
      component.createTask();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/operations/complementary-tasks/create']);
    });
  });

  describe('editTask', () => {
    it('should navigate to edit form with task id', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';

      component.editTask(taskId);

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/operations/complementary-tasks/edit', taskId]);
    });
  });

  describe('completeTask', () => {
    it('should mark task as completed after confirmation', () => {
      spyOn(window, 'confirm').and.returnValue(true);
      mockService.complete.and.returnValue(of(mockTask));
      mockService.getAll.and.returnValue(of([mockTask]));

      component.completeTask(mockTask);

      expect(mockService.complete).toHaveBeenCalledWith(mockTask.id);
      expect(component.successMessage).toBe('Task marked as completed');
    });

    it('should not complete task if user cancels', () => {
      spyOn(window, 'confirm').and.returnValue(false);

      component.completeTask(mockTask);

      expect(mockService.complete).not.toHaveBeenCalled();
    });

    it('should handle error when completing task', () => {
      spyOn(window, 'confirm').and.returnValue(true);
      const error = { error: { message: 'Failed to complete' } };
      mockService.complete.and.returnValue(throwError(() => error));

      component.completeTask(mockTask);

      expect(component.errorMessage).toBe('Failed to complete');
    });
  });

  describe('deleteTask', () => {
    it('should delete task after confirmation', () => {
      spyOn(window, 'confirm').and.returnValue(true);
      mockService.delete.and.returnValue(of(void 0));
      mockService.getAll.and.returnValue(of([]));

      component.deleteTask(mockTask);

      expect(mockService.delete).toHaveBeenCalledWith(mockTask.id);
      expect(component.successMessage).toBe('Task deleted successfully');
    });

    it('should not delete task if user cancels', () => {
      spyOn(window, 'confirm').and.returnValue(false);

      component.deleteTask(mockTask);

      expect(mockService.delete).not.toHaveBeenCalled();
    });

    it('should handle error when deleting task', () => {
      spyOn(window, 'confirm').and.returnValue(true);
      const error = { error: { message: 'Failed to delete' } };
      mockService.delete.and.returnValue(throwError(() => error));

      component.deleteTask(mockTask);

      expect(component.errorMessage).toBe('Failed to delete');
    });
  });

  describe('pagination', () => {
    beforeEach(() => {
      const tasks = Array.from({ length: 25 }, (_, i) => ({
        ...mockTask,
        id: `task-${i}`
      }));
      component.filteredTasks = tasks;
      component.pageSize = 10;
    });

    it('should return correct paginated tasks', () => {
      component.currentPage = 1;
      const paginated = component.paginatedTasks;

      expect(paginated.length).toBe(10);
      expect(paginated[0].id).toBe('task-0');
    });

    it('should calculate total pages correctly', () => {
      expect(component.totalPages).toBe(3);
    });

    it('should navigate to valid page', () => {
      component.goToPage(2);
      expect(component.currentPage).toBe(2);
    });

    it('should not navigate to invalid page', () => {
      component.currentPage = 1;
      component.goToPage(10);
      expect(component.currentPage).toBe(1);
    });
  });

  describe('helper methods', () => {
    it('should return correct status class', () => {
      expect(component.getStatusClass('Ongoing')).toBe('badge-warning');
      expect(component.getStatusClass('Completed')).toBe('badge-success');
    });

    it('should return correct impact class', () => {
      expect(component.getImpactClass('Blocking')).toBe('badge-danger');
      expect(component.getImpactClass('Parallel')).toBe('badge-info');
    });

    it('should format date correctly', () => {
      const dateString = '2025-01-15T10:00:00Z';
      const formatted = component.formatDate(dateString);
      expect(formatted).toContain('2025');
    });

    it('should format duration correctly', () => {
      expect(component.formatDuration(90)).toBe('1h 30m');
      expect(component.formatDuration(45)).toBe('45m');
    });
  });
});
