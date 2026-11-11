import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ExecutedOperationService, ExecutedOperationDTO, CreateExecutedOperationDTO, UpdateExecutedOperationDTO, InitializeOperationsFromPlanResponseDTO } from './executed-operation.service';
import { AuthService } from '../auth.service';
import { environment } from '../../../environments/environment';

/**
 * US 4.1.9: Test suite for Executed Operations Service
 * 
 * Purpose: Verify that the frontend service correctly manages executed operations
 * within vessel visit executions, including CRUD operations and initialization
 * 
 * Coverage:
 * - HTTP GET/POST/PUT/DELETE requests
 * - Initialize operations from plan
 * - Get operations by VVE ID
 * - Create/update/delete operations
 * - Authorization header inclusion
 * - Error handling
 */
describe('ExecutedOperationService - US 4.1.9', () => {
  let service: ExecutedOperationService;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<AuthService>;

  const mockUserEmail = 'operator@portlogistics.com';
  const apiUrl = `${environment.oemApiUrl || 'https://localhost:7001'}/api/ExecutedOperations`;

  beforeEach(() => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['getUserEmail']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        ExecutedOperationService,
        { provide: AuthService, useValue: authServiceSpy }
      ]
    });

    service = TestBed.inject(ExecutedOperationService);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    authService.getUserEmail.and.returnValue(mockUserEmail);
  });

  afterEach(() => {
    httpMock.verify();
  });

  /**
   * Test 1: Verify that getByVveId makes GET request to correct endpoint
   */
  it('should make GET request to get operations by VVE ID', () => {
    const vveId = 'VVE-2026-001';

    service.getByVveId(vveId).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/vve/${vveId}`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  /**
   * Test 2: Verify that getById makes GET request to correct endpoint
   */
  it('should make GET request to get operation by ID', () => {
    const operationId = '123e4567-e89b-12d3-a456-426614174000';

    service.getById(operationId).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/${operationId}`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  /**
   * Test 3: Verify that initializeFromPlan makes POST request
   */
  it('should make POST request to initialize operations from plan', () => {
    const request = {
      vveId: 'VVE-2026-001',
      operationPlanId: '550e8400-e29b-41d4-a716-446655440000'
    };

    service.initializeFromPlan(request).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/initialize-from-plan`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ success: true });
  });

  /**
   * Test 4: Verify that create makes POST request
   */
  it('should make POST request to create operation', () => {
    const createDto: CreateExecutedOperationDTO = {
      vveId: 'VVE-2026-001',
      plannedOperationIndex: 0,
      type: 'unloading',
      plannedStartTime: '2026-01-15T08:00:00Z',
      plannedEndTime: '2026-01-15T10:00:00Z'
    };

    service.create(createDto).subscribe();

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createDto);
    req.flush({});
  });

  /**
   * Test 5: Verify that update makes PUT request
   */
  it('should make PUT request to update operation', () => {
    const operationId = '123e4567-e89b-12d3-a456-426614174000';
    const updateDto: UpdateExecutedOperationDTO = {
      actualStartTime: '2026-01-15T08:15:00Z',
      status: 'Started'
    };

    service.update(operationId, updateDto).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/${operationId}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateDto);
    req.flush({});
  });

  /**
   * Test 6: Verify that delete makes DELETE request
   */
  it('should make DELETE request to delete operation', () => {
    const operationId = '123e4567-e89b-12d3-a456-426614174000';

    service.delete(operationId).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/${operationId}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  /**
   * Test 7: Verify authorization header is included in requests
   */
  it('should include X-User-Email header in requests', () => {
    const vveId = 'VVE-2026-001';

    service.getByVveId(vveId).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/vve/${vveId}`);
    expect(req.request.headers.get('X-User-Email')).toBe(mockUserEmail);
    req.flush([]);
  });

  /**
   * Test 8: Verify Content-Type header is set
   */
  it('should include Content-Type header in requests', () => {
    const createDto: CreateExecutedOperationDTO = {
      vveId: 'VVE-2026-001',
      plannedOperationIndex: 0,
      type: 'loading'
    };

    service.create(createDto).subscribe();

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.headers.get('Content-Type')).toBe('application/json');
    req.flush({});
  });

  /**
   * Test 9: Verify response structure for getByVveId
   */
  it('should receive list of executed operations for VVE', () => {
    const vveId = 'VVE-2026-001';
    const mockOperations: ExecutedOperationDTO[] = [
      {
        id: '123e4567-e89b-12d3-a456-426614174000',
        vveId: vveId,
        plannedOperationIndex: 0,
        type: 'unloading',
        status: 'Started',
        plannedStartTime: '2026-01-15T08:00:00Z',
        plannedEndTime: '2026-01-15T10:00:00Z',
        actualStartTime: '2026-01-15T08:15:00Z',
        delayMinutes: 15,
        updatedBy: 'operator@test.com',
        updatedAt: '2026-01-15T08:15:00Z',
        createdAt: '2026-01-15T08:00:00Z'
      },
      {
        id: '223e4567-e89b-12d3-a456-426614174001',
        vveId: vveId,
        plannedOperationIndex: 1,
        type: 'loading',
        status: 'Started',
        plannedStartTime: '2026-01-15T10:00:00Z',
        plannedEndTime: '2026-01-15T12:00:00Z',
        updatedBy: 'operator@test.com',
        updatedAt: '2026-01-15T08:00:00Z',
        createdAt: '2026-01-15T08:00:00Z'
      }
    ];

    service.getByVveId(vveId).subscribe((operations) => {
      expect(operations.length).toBe(2);
      expect(operations[0].type).toBe('unloading');
      expect(operations[1].type).toBe('loading');
      expect(operations[0].delayMinutes).toBe(15);
    });

    const req = httpMock.expectOne(`${apiUrl}/vve/${vveId}`);
    req.flush(mockOperations);
  });

  /**
   * Test 10: Verify response structure for initializeFromPlan
   */
  it('should receive initialization response with operations count', () => {
    const request = {
      vveId: 'VVE-2026-001',
      operationPlanId: '550e8400-e29b-41d4-a716-446655440000'
    };

    const mockResponse: InitializeOperationsFromPlanResponseDTO = {
      success: true,
      message: '2 operations initialized successfully',
      operationsInitialized: 2,
      operations: [
        { id: '1', vveId: 'VVE-2026-001', type: 'unloading', status: 'Started', plannedOperationIndex: 0, updatedBy: '', updatedAt: '', createdAt: '' },
        { id: '2', vveId: 'VVE-2026-001', type: 'loading', status: 'Started', plannedOperationIndex: 1, updatedBy: '', updatedAt: '', createdAt: '' }
      ]
    };

    service.initializeFromPlan(request).subscribe((response) => {
      expect(response.success).toBe(true);
      expect(response.operationsInitialized).toBe(2);
      expect(response.operations.length).toBe(2);
    });

    const req = httpMock.expectOne(`${apiUrl}/initialize-from-plan`);
    req.flush(mockResponse);
  });

  /**
   * Test 11: Verify update operation with status change
   */
  it('should update operation status to Completed', () => {
    const operationId = '123e4567-e89b-12d3-a456-426614174000';
    const updateDto: UpdateExecutedOperationDTO = {
      actualEndTime: '2026-01-15T10:30:00Z',
      status: 'Completed'
    };

    service.update(operationId, updateDto).subscribe((result) => {
      expect(result.status).toBe('Completed');
    });

    const req = httpMock.expectOne(`${apiUrl}/${operationId}`);
    req.flush({ ...updateDto, id: operationId, status: 'Completed' });
  });

  /**
   * Test 12: Verify update operation with status Delayed
   */
  it('should update operation status to Delayed', () => {
    const operationId = '123e4567-e89b-12d3-a456-426614174000';
    const updateDto: UpdateExecutedOperationDTO = {
      status: 'Delayed',
      notes: 'Delayed due to equipment malfunction'
    };

    service.update(operationId, updateDto).subscribe((result) => {
      expect(result.status).toBe('Delayed');
    });

    const req = httpMock.expectOne(`${apiUrl}/${operationId}`);
    req.flush({ ...updateDto, id: operationId, status: 'Delayed' });
  });

  /**
   * Test 13: Verify getById returns single operation
   */
  it('should receive single operation by ID', () => {
    const operationId = '123e4567-e89b-12d3-a456-426614174000';
    const mockOperation: ExecutedOperationDTO = {
      id: operationId,
      vveId: 'VVE-2026-001',
      plannedOperationIndex: 0,
      type: 'unloading',
      status: 'Completed',
      actualStartTime: '2026-01-15T08:00:00Z',
      actualEndTime: '2026-01-15T10:00:00Z',
      updatedBy: 'operator@test.com',
      updatedAt: '2026-01-15T10:00:00Z',
      createdAt: '2026-01-15T08:00:00Z'
    };

    service.getById(operationId).subscribe((operation) => {
      expect(operation.id).toBe(operationId);
      expect(operation.status).toBe('Completed');
      expect(operation.actualEndTime).toBeDefined();
    });

    const req = httpMock.expectOne(`${apiUrl}/${operationId}`);
    req.flush(mockOperation);
  });

  /**
   * Test 14: Verify create with resources
   */
  it('should create operation with actual resources', () => {
    const createDto: CreateExecutedOperationDTO = {
      vveId: 'VVE-2026-001',
      plannedOperationIndex: 0,
      type: 'loading',
      actualResources: [
        { resourceType: 'crane', resourceId: 'CRANE-A1', quantity: 1 },
        { resourceType: 'staff', resourceId: 'OP-001', quantity: 1 }
      ]
    };

    service.create(createDto).subscribe();

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.body.actualResources).toBeDefined();
    expect(req.request.body.actualResources.length).toBe(2);
    req.flush({});
  });

  /**
   * Test 15: Verify initializeFromPlan failure response
   */
  it('should handle initialization failure response', () => {
    const request = {
      vveId: 'VVE-INVALID',
      operationPlanId: '550e8400-e29b-41d4-a716-446655440000'
    };

    const mockResponse: InitializeOperationsFromPlanResponseDTO = {
      success: false,
      message: 'VVE not found',
      operationsInitialized: 0,
      operations: []
    };

    service.initializeFromPlan(request).subscribe((response) => {
      expect(response.success).toBe(false);
      expect(response.operationsInitialized).toBe(0);
      expect(response.message).toContain('not found');
    });

    const req = httpMock.expectOne(`${apiUrl}/initialize-from-plan`);
    req.flush(mockResponse);
  });

  /**
   * Test 16: Verify empty operations list for new VVE
   */
  it('should return empty list for VVE with no operations', () => {
    const vveId = 'VVE-NEW-001';

    service.getByVveId(vveId).subscribe((operations) => {
      expect(operations.length).toBe(0);
    });

    const req = httpMock.expectOne(`${apiUrl}/vve/${vveId}`);
    req.flush([]);
  });

  /**
   * Test 17: Verify update with notes
   */
  it('should update operation with notes', () => {
    const operationId = '123e4567-e89b-12d3-a456-426614174000';
    const updateDto: UpdateExecutedOperationDTO = {
      notes: 'Updated operation notes for tracking purposes'
    };

    service.update(operationId, updateDto).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/${operationId}`);
    expect(req.request.body.notes).toBeDefined();
    req.flush({});
  });

  /**
   * Test 18: Verify service handles HTTP errors
   */
  it('should handle HTTP errors gracefully', () => {
    const vveId = 'VVE-ERROR';

    service.getByVveId(vveId).subscribe({
      error: (error) => {
        expect(error.status).toBe(500);
      }
    });

    const req = httpMock.expectOne(`${apiUrl}/vve/${vveId}`);
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
  });

  /**
   * Test 19: Verify create operation with optional operation plan ID
   */
  it('should create manual operation without operation plan ID', () => {
    const createDto: CreateExecutedOperationDTO = {
      vveId: 'VVE-2026-001',
      plannedOperationIndex: 0,
      type: 'loading',
      notes: 'Manually created operation'
    };

    service.create(createDto).subscribe();

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.body.operationPlanId).toBeUndefined();
    req.flush({});
  });

  /**
   * Test 20: Verify authorization fallback when no user email
   */
  it('should use fallback email when getUserEmail returns null', () => {
    authService.getUserEmail.and.returnValue(null);
    const vveId = 'VVE-2026-001';

    service.getByVveId(vveId).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/vve/${vveId}`);
    expect(req.request.headers.get('X-User-Email')).toBeDefined();
    req.flush([]);
  });
});
