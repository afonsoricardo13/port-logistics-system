import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { VesselVisitExecutionService, VesselVisitExecutionSearchResultDTO } from './vessel-visit-execution.service';
import { AuthService } from '../auth.service';
import { environment } from '../../../environments/environment';

/**
 * US 4.1.10: Test suite for Search and List VVEs with Execution Metrics
 * 
 * Purpose: Verify that the frontend service correctly searches and filters
 * Vessel Visit Executions with performance metrics
 * 
 * Coverage:
 * - HTTP GET requests with query parameters
 * - Date range filtering
 * - Vessel IMO filtering
 * - Status filtering
 * - Response structure validation with metrics
 * - Authorization header inclusion
 * - Error handling
 */
describe('VesselVisitExecutionService - US 4.1.10', () => {
  let service: VesselVisitExecutionService;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<AuthService>;

  const mockUserEmail = 'operator@portlogistics.com';
  const apiUrl = `${environment.oemApiUrl}/api/VesselVisitExecutions`;

  beforeEach(() => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['getUserEmail']);
    
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        VesselVisitExecutionService,
        { provide: AuthService, useValue: authServiceSpy }
      ]
    });

    service = TestBed.inject(VesselVisitExecutionService);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    authService.getUserEmail.and.returnValue(mockUserEmail);
  });

  afterEach(() => {
    httpMock.verify();
  });

  /**
   * Test 1: Verify that searchWithMetrics makes GET request to correct endpoint
   * 
   * Validates that the service constructs the search URL correctly
   * and includes all query parameters
   */
  it('should make GET request to search endpoint with query parameters', () => {
    const startDate = '2024-01-01';
    const endDate = '2024-12-31';
    const vesselImo = 'IMO9876543';
    const status = 'Completed';

    service.searchWithMetrics(startDate, endDate, vesselImo, status).subscribe();

    const req = httpMock.expectOne((request) => {
      return request.url === `${apiUrl}/search` &&
        request.params.get('startDate') === startDate &&
        request.params.get('endDate') === endDate &&
        request.params.get('vesselImo') === vesselImo &&
        request.params.get('status') === status;
    });

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  /**
   * Test 2: Verify that date range filters are correctly included
   * 
   * Ensures startDate and endDate parameters are properly
   * sent to the backend for temporal filtering
   */
  it('should include date range parameters when searching by date', () => {
    const startDate = '2024-06-01T00:00:00Z';
    const endDate = '2024-06-30T23:59:59Z';

    service.searchWithMetrics(startDate, endDate).subscribe();

    const req = httpMock.expectOne((request) => {
      return request.url.includes('/search') &&
        request.params.has('startDate') &&
        request.params.has('endDate');
    });

    expect(req.request.params.get('startDate')).toBe(startDate);
    expect(req.request.params.get('endDate')).toBe(endDate);
    req.flush([]);
  });

  /**
   * Test 3: Verify vessel IMO filter parameter
   * 
   * Confirms that vessel-specific searches include the
   * vesselImo parameter in the request
   */
  it('should include vesselImo parameter when searching by vessel', () => {
    const vesselImo = 'IMO1234567';

    service.searchWithMetrics(undefined, undefined, vesselImo).subscribe();

    const req = httpMock.expectOne((request) => {
      return request.url.includes('/search') &&
        request.params.get('vesselImo') === vesselImo;
    });

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  /**
   * Test 4: Verify status filter parameter
   * 
   * Ensures execution status filtering is correctly
   * passed to the backend
   */
  it('should include status parameter when filtering by status', () => {
    const status = 'InProgress';

    service.searchWithMetrics(undefined, undefined, undefined, status).subscribe();

    const req = httpMock.expectOne((request) => {
      return request.url.includes('/search') &&
        request.params.get('status') === status;
    });

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  /**
   * Test 5: Verify response structure includes execution metrics
   * 
   * Validates that the API response contains all required
   * performance metrics for execution analysis
   */
  it('should receive search results with execution metrics', () => {
    const mockResults: VesselVisitExecutionSearchResultDTO[] = [
      {
        id: 'vve-123',
        vvnId: 'VVN-001',
        vesselImo: 'IMO9876543',
        vesselName: 'MV Atlantic Express',
        actualArrivalTime: '2024-06-15T08:00:00Z',
        actualBerthTime: '2024-06-15T09:30:00Z',
        dockUsed: 'DOCK-A1',
        actualUnberthTime: '2024-06-16T14:00:00Z',
        actualDepartureTime: '2024-06-16T15:30:00Z',
        status: 'Completed',
        expectedArrivalTime: '2024-06-15T08:00:00Z',
        expectedDepartureTime: '2024-06-16T16:00:00Z',
        totalTurnaroundTimeHours: 31.5,
        berthOccupancyTimeHours: 28.5,
        waitingTimeForBerthingHours: 1.5,
        arrivalDelayHours: 0.0,
        departureDelayHours: 0.5,
        executedOperationsCount: 12,
        createdAt: '2024-06-15T08:00:00Z',
        notes: 'Normal operations'
      }
    ];

    service.searchWithMetrics('2024-06-01', '2024-06-30').subscribe((results) => {
      expect(results.length).toBe(1);
      expect(results[0].totalTurnaroundTimeHours).toBe(31.5);
      expect(results[0].berthOccupancyTimeHours).toBe(28.5);
      expect(results[0].waitingTimeForBerthingHours).toBe(1.5);
      expect(results[0].arrivalDelayHours).toBe(0.0);
      expect(results[0].departureDelayHours).toBe(0.5);
      expect(results[0].executedOperationsCount).toBe(12);
    });

    const req = httpMock.expectOne((request) => request.url.includes('/search'));
    req.flush(mockResults);
  });

  /**
   * Test 6: Verify authorization header is included
   * 
   * Confirms that the X-User-Email header is sent with
   * every search request for authentication
   */
  it('should include X-User-Email header in search requests', () => {
    service.searchWithMetrics().subscribe();

    const req = httpMock.expectOne(`${apiUrl}/search`);
    expect(req.request.headers.get('X-User-Email')).toBe(mockUserEmail);
    req.flush([]);
  });

  /**
   * Test 7: Verify search with no parameters works
   * 
   * Ensures that calling searchWithMetrics without filters
   * retrieves all VVEs without query parameters
   */
  it('should handle search with no filters', () => {
    service.searchWithMetrics().subscribe();

    const req = httpMock.expectOne((request) => {
      return request.url === `${apiUrl}/search` &&
        !request.params.has('startDate') &&
        !request.params.has('endDate') &&
        !request.params.has('vesselImo') &&
        !request.params.has('status');
    });

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  /**
   * Test 8: Verify combined filters work correctly
   * 
   * Tests that multiple filter parameters can be used
   * simultaneously for advanced search scenarios
   */
  it('should support combined filters for advanced search', () => {
    const filters = {
      startDate: '2024-01-01',
      endDate: '2024-03-31',
      vesselImo: 'IMO5555555',
      status: 'Completed'
    };

    service.searchWithMetrics(
      filters.startDate,
      filters.endDate,
      filters.vesselImo,
      filters.status
    ).subscribe();

    const req = httpMock.expectOne((request) => {
      return request.params.get('startDate') === filters.startDate &&
        request.params.get('endDate') === filters.endDate &&
        request.params.get('vesselImo') === filters.vesselImo &&
        request.params.get('status') === filters.status;
    });

    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  /**
   * Test 9: Verify response with multiple VVEs
   * 
   * Validates that the service correctly handles responses
   * containing multiple vessel visit executions
   */
  it('should handle multiple search results', () => {
    const mockResults: VesselVisitExecutionSearchResultDTO[] = [
      {
        id: 'vve-1',
        vvnId: 'VVN-001',
        vesselImo: 'IMO1111111',
        vesselName: 'Vessel One',
        status: 'Completed',
        totalTurnaroundTimeHours: 24.0
      },
      {
        id: 'vve-2',
        vvnId: 'VVN-002',
        vesselImo: 'IMO2222222',
        vesselName: 'Vessel Two',
        status: 'Completed',
        totalTurnaroundTimeHours: 36.0
      },
      {
        id: 'vve-3',
        vvnId: 'VVN-003',
        vesselImo: 'IMO3333333',
        vesselName: 'Vessel Three',
        status: 'InProgress',
        totalTurnaroundTimeHours: undefined
      }
    ];

    service.searchWithMetrics().subscribe((results) => {
      expect(results.length).toBe(3);
      expect(results[0].vesselImo).toBe('IMO1111111');
      expect(results[1].vesselImo).toBe('IMO2222222');
      expect(results[2].status).toBe('InProgress');
    });

    const req = httpMock.expectOne(`${apiUrl}/search`);
    req.flush(mockResults);
  });

  /**
   * Test 10: Verify error handling for failed search requests
   * 
   * Ensures that HTTP errors are properly propagated
   * for UI error handling
   */
  it('should handle HTTP errors gracefully', () => {
    const errorMessage = 'Failed to retrieve search results';

    service.searchWithMetrics().subscribe({
      next: () => fail('should have failed with 500 error'),
      error: (error) => {
        expect(error.status).toBe(500);
        expect(error.statusText).toBe('Internal Server Error');
      }
    });

    const req = httpMock.expectOne(`${apiUrl}/search`);
    req.flush(errorMessage, { status: 500, statusText: 'Internal Server Error' });
  });
});
