import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ComplementaryTasksService, ComplementaryTaskDTO, CreateComplementaryTaskDTO, UpdateComplementaryTaskDTO } from './complementary-tasks.service';
import { AuthService } from './auth.service';

describe('ComplementaryTasksService', () => {
  let service: ComplementaryTasksService;
  let httpMock: HttpTestingController;
  let authServiceMock: jasmine.SpyObj<AuthService>;

  const mockTask: ComplementaryTaskDTO = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    categoryCode: 'CTC001',
    categoryName: 'Inspection',
    vveId: 'VVE-001',
    vesselName: 'Test Vessel',
    responsibleTeam: 'Team A',
    startTime: '2025-01-15T10:00:00Z',
    endTime: undefined,
    status: 'Ongoing',
    impactType: 'Parallel',
    notes: 'Test notes',
    createdAt: '2025-01-15T09:00:00Z',
    isOngoing: true
  };

  beforeEach(() => {
    authServiceMock = jasmine.createSpyObj('AuthService', ['getUserEmail']);
    authServiceMock.getUserEmail.and.returnValue('test@example.com');

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        ComplementaryTasksService,
        { provide: AuthService, useValue: authServiceMock }
      ]
    });

    service = TestBed.inject(ComplementaryTasksService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAll', () => {
    it('should fetch all tasks without filters', () => {
      const mockTasks = [mockTask];

      service.getAll().subscribe(tasks => {
        expect(tasks).toEqual(mockTasks);
        expect(tasks.length).toBe(1);
      });

      const req = httpMock.expectOne(request => 
        request.url.includes('/api/ComplementaryTasks') && request.params.keys().length === 0
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockTasks);
    });

    it('should fetch tasks with status filter', () => {
      const mockTasks = [mockTask];

      service.getAll({ status: 'Ongoing' }).subscribe(tasks => {
        expect(tasks).toEqual(mockTasks);
      });

      const req = httpMock.expectOne(request => 
        request.url.includes('/api/ComplementaryTasks') && 
        request.params.get('status') === 'Ongoing'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockTasks);
    });

    it('should fetch tasks with vveId filter', () => {
      service.getAll({ vveId: 'VVE-001' }).subscribe();

      const req = httpMock.expectOne(request => 
        request.params.get('vveId') === 'VVE-001'
      );
      expect(req.request.method).toBe('GET');
      req.flush([mockTask]);
    });

    it('should fetch tasks with date range filter', () => {
      service.getAll({ 
        startDate: '2025-01-01', 
        endDate: '2025-01-31' 
      }).subscribe();

      const req = httpMock.expectOne(request => 
        request.params.get('startDate') === '2025-01-01' &&
        request.params.get('endDate') === '2025-01-31'
      );
      expect(req.request.method).toBe('GET');
      req.flush([mockTask]);
    });

    it('should fetch tasks with ongoingOnly filter', () => {
      service.getAll({ ongoingOnly: true }).subscribe();

      const req = httpMock.expectOne(request => 
        request.params.get('ongoingOnly') === 'true'
      );
      expect(req.request.method).toBe('GET');
      req.flush([mockTask]);
    });

    it('should fetch tasks with impactType filter', () => {
      service.getAll({ impactType: 'Blocking' }).subscribe();

      const req = httpMock.expectOne(request => 
        request.params.get('impactType') === 'Blocking'
      );
      expect(req.request.method).toBe('GET');
      req.flush([mockTask]);
    });
  });

  describe('getOngoing', () => {
    it('should fetch only ongoing tasks', () => {
      const mockTasks = [mockTask];

      service.getOngoing().subscribe(tasks => {
        expect(tasks).toEqual(mockTasks);
        expect(tasks.every(t => t.status === 'Ongoing')).toBe(true);
      });

      const req = httpMock.expectOne(request => 
        request.url.includes('/api/ComplementaryTasks/ongoing')
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockTasks);
    });
  });

  describe('getById', () => {
    it('should fetch task by id', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';

      service.getById(taskId).subscribe(task => {
        expect(task).toEqual(mockTask);
        expect(task.id).toBe(taskId);
      });

      const req = httpMock.expectOne(request => 
        request.url.includes(`/api/ComplementaryTasks/${taskId}`)
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockTask);
    });
  });

  describe('getByVveId', () => {
    it('should fetch tasks by VVE id', () => {
      const vveId = 'VVE-001';
      const mockTasks = [mockTask];

      service.getByVveId(vveId).subscribe(tasks => {
        expect(tasks).toEqual(mockTasks);
        expect(tasks.every(t => t.vveId === vveId)).toBe(true);
      });

      const req = httpMock.expectOne(request => 
        request.url.includes(`/api/ComplementaryTasks/vve/${vveId}`)
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockTasks);
    });
  });

  describe('create', () => {
    it('should create a new task', () => {
      const createDto: CreateComplementaryTaskDTO = {
        categoryCode: 'CTC001',
        vveId: 'VVE-001',
        responsibleTeam: 'Team A',
        startTime: '2025-01-15T10:00:00Z',
        impactType: 'Parallel',
        notes: 'Test notes'
      };

      service.create(createDto).subscribe(task => {
        expect(task).toEqual(mockTask);
        expect(task.status).toBe('Ongoing');
      });

      const req = httpMock.expectOne(request => 
        request.url.includes('/api/ComplementaryTasks') &&
        request.method === 'POST'
      );
      expect(req.request.body).toEqual(createDto);
      req.flush(mockTask);
    });
  });

  describe('update', () => {
    it('should update an existing task', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';
      const updateDto: UpdateComplementaryTaskDTO = {
        responsibleTeam: 'Team B',
        notes: 'Updated notes'
      };

      const updatedTask = { ...mockTask, responsibleTeam: 'Team B' };

      service.update(taskId, updateDto).subscribe(task => {
        expect(task.responsibleTeam).toBe('Team B');
      });

      const req = httpMock.expectOne(request => 
        request.url.includes(`/api/ComplementaryTasks/${taskId}`) &&
        request.method === 'PUT'
      );
      expect(req.request.body).toEqual(updateDto);
      req.flush(updatedTask);
    });
  });

  describe('complete', () => {
    it('should mark task as completed with current time', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';
      const completedTask = { 
        ...mockTask, 
        status: 'Completed' as const,
        endTime: '2025-01-15T12:00:00Z'
      };

      service.complete(taskId).subscribe(task => {
        expect(task.status).toBe('Completed');
        expect(task.endTime).toBeDefined();
      });

      const req = httpMock.expectOne(request => 
        request.url.includes(`/api/ComplementaryTasks/${taskId}`) &&
        request.method === 'PUT'
      );
      expect(req.request.body.status).toBe('Completed');
      expect(req.request.body.endTime).toBeDefined();
      req.flush(completedTask);
    });

    it('should mark task as completed with specified time', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';
      const endTime = '2025-01-15T12:00:00Z';

      service.complete(taskId, endTime).subscribe();

      const req = httpMock.expectOne(request => 
        request.url.includes(`/api/ComplementaryTasks/${taskId}`)
      );
      expect(req.request.body.endTime).toBe(endTime);
      req.flush({ ...mockTask, status: 'Completed', endTime });
    });
  });

  describe('delete', () => {
    it('should delete a task', () => {
      const taskId = '123e4567-e89b-12d3-a456-426614174000';

      service.delete(taskId).subscribe(response => {
        expect(response).toBeNull();
      });

      const req = httpMock.expectOne(request => 
        request.url.includes(`/api/ComplementaryTasks/${taskId}`) &&
        request.method === 'DELETE'
      );
      req.flush(null);
    });
  });

  describe('headers', () => {
    it('should include user email in headers', () => {
      service.getAll().subscribe();

      const req = httpMock.expectOne(request => 
        request.url.includes('/api/ComplementaryTasks')
      );
      expect(req.request.headers.get('X-User-Email')).toBe('test@example.com');
      req.flush([]);
    });

    it('should use default email when user email is not available', () => {
      authServiceMock.getUserEmail.and.returnValue(null);

      service.getAll().subscribe();

      const req = httpMock.expectOne(request => 
        request.url.includes('/api/ComplementaryTasks')
      );
      expect(req.request.headers.get('X-User-Email')).toBe('system@portlogistics.com');
      req.flush([]);
    });
  });
});
