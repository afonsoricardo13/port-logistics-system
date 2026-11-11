/**
 * US 4.1.5: Unit Tests for Operation Planning Service - Get VVNs Without Operation Plans
 * 
 * These tests validate the frontend service layer's interaction with the OEM API
 * for retrieving VVNs that don't have operation plans assigned for a specific date.
 * Tests cover HTTP requests, response handling, error scenarios, and data validation.
 */

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { OperationPlanningService, VvnsWithoutPlansResponse } from './operation-planning.service';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

describe('OperationPlanningService - US 4.1.5 getVvnsWithoutPlans', () => {
  let service: OperationPlanningService;
  let httpMock: HttpTestingController;
  let mockAuthService: jasmine.SpyObj<AuthService>;

  const OEM_API_URL = `${environment.oemApiUrl || 'https://localhost:7001'}/api/OperationPlans`;

  beforeEach(() => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['getAccessToken']);
    mockAuthService.getAccessToken.and.returnValue('mock-token');

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        OperationPlanningService,
        { provide: AuthService, useValue: mockAuthService }
      ]
    });

    service = TestBed.inject(OperationPlanningService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  /**
   * Test 1: Validates successful retrieval of VVNs without operation plans
   * Verifies that the service correctly calls the API and parses the response
   * when multiple VVNs are missing plans
   */
  it('should fetch VVNs without operation plans', (done) => {
    const testDate = '2026-01-15';
    const mockResponse: VvnsWithoutPlansResponse = {
      success: true,
      message: 'Found 2 VVN(s) without Operation Plans',
      date: testDate,
      totalApprovedVvns: 3,
      vvnsWithPlans: 1,
      vvnsWithoutPlans: 2,
      missingPlans: [
        {
          id: 1,
          vesselImoNumber: 'IMO1234567',
          vesselName: 'Test Vessel 1',
          estimatedTimeOfArrival: '2026-01-15T08:00:00Z',
          estimatedTimeOfDeparture: '2026-01-15T16:00:00Z',
          assignedDockCode: 'DOCK-A'
        },
        {
          id: 2,
          vesselImoNumber: 'IMO2345678',
          vesselName: 'Test Vessel 2',
          estimatedTimeOfArrival: '2026-01-15T10:00:00Z',
          estimatedTimeOfDeparture: '2026-01-15T18:00:00Z',
          assignedDockCode: 'DOCK-B'
        }
      ]
    };

    service.getVvnsWithoutPlans(testDate).subscribe(response => {
      expect(response.success).toBe(true);
      expect(response.vvnsWithoutPlans).toBe(2);
      expect(response.missingPlans.length).toBe(2);
      expect(response.missingPlans[0].id).toBe(1);
      expect(response.missingPlans[1].id).toBe(2);
      done();
    });

    const req = httpMock.expectOne(`${OEM_API_URL}/missing-plans?date=${testDate}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  /**
   * Test 2: Tests the happy path scenario where all VVNs have operation plans
   * Expects an empty missingPlans array and appropriate success message
   */
  it('should return empty list when all VVNs have plans', (done) => {
    const testDate = '2026-01-15';
    const mockResponse: VvnsWithoutPlansResponse = {
      success: true,
      message: 'All Approved VVNs have Operation Plans',
      date: testDate,
      totalApprovedVvns: 2,
      vvnsWithPlans: 2,
      vvnsWithoutPlans: 0,
      missingPlans: []
    };

    service.getVvnsWithoutPlans(testDate).subscribe(response => {
      expect(response.success).toBe(true);
      expect(response.vvnsWithoutPlans).toBe(0);
      expect(response.missingPlans.length).toBe(0);
      expect(response.message).toContain('All Approved VVNs have Operation Plans');
      done();
    });

    const req = httpMock.expectOne(`${OEM_API_URL}/missing-plans?date=${testDate}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  /**
   * Test 3: Handles edge case where no approved VVNs exist for the given date
   * Service should gracefully return zero counts across all statistics
   */
  it('should handle empty response when no approved VVNs exist', (done) => {
    const testDate = '2026-01-15';
    const mockResponse: VvnsWithoutPlansResponse = {
      success: true,
      message: 'No Approved VVNs found',
      date: testDate,
      totalApprovedVvns: 0,
      vvnsWithPlans: 0,
      vvnsWithoutPlans: 0,
      missingPlans: []
    };

    service.getVvnsWithoutPlans(testDate).subscribe(response => {
      expect(response.success).toBe(true);
      expect(response.totalApprovedVvns).toBe(0);
      expect(response.missingPlans).toEqual([]);
      done();
    });

    const req = httpMock.expectOne(`${OEM_API_URL}/missing-plans?date=${testDate}`);
    req.flush(mockResponse);
  });

  /**
   * Test 4: Validates proper error handling when the API returns HTTP error responses
   * Service should propagate errors to allow component-level error handling
   */
  it('should handle API errors', (done) => {
    const testDate = '2026-01-15';
    const errorMessage = 'Error retrieving VVNs without Operation Plans';

    service.getVvnsWithoutPlans(testDate).subscribe({
      next: () => fail('should have failed with 400 error'),
      error: (error) => {
        expect(error.status).toBe(400);
        expect(error.statusText).toBe('Bad Request');
        done();
      }
    });

    const req = httpMock.expectOne(`${OEM_API_URL}/missing-plans?date=${testDate}`);
    req.flush({ success: false, message: errorMessage }, { status: 400, statusText: 'Bad Request' });
  });

  /**
   * Test 5: Tests resilience against network-level failures (timeouts, connection errors)
   * Ensures the service properly propagates ProgressEvent errors
   */
  it('should handle network errors', (done) => {
    const testDate = '2026-01-15';

    service.getVvnsWithoutPlans(testDate).subscribe({
      next: () => fail('should have failed with network error'),
      error: (error) => {
        expect(error.error.type).toBe('error');
        done();
      }
    });

    const req = httpMock.expectOne(`${OEM_API_URL}/missing-plans?date=${testDate}`);
    req.error(new ProgressEvent('error'));
  });

  /**
   * Test 6: Verifies that the date is correctly formatted and included
   * as a query parameter in the GET request URL
   */
  it('should apply correct query parameters', (done) => {
    const testDate = '2026-01-15';
    const mockResponse: VvnsWithoutPlansResponse = {
      success: true,
      message: '',
      date: testDate,
      totalApprovedVvns: 0,
      vvnsWithPlans: 0,
      vvnsWithoutPlans: 0,
      missingPlans: []
    };

    service.getVvnsWithoutPlans(testDate).subscribe(() => {
      done();
    });

    const req = httpMock.expectOne(`${OEM_API_URL}/missing-plans?date=${testDate}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  /**
   * Test 7: Ensures the service includes the X-User-Email header for authentication
   * This is required by the backend to track who is making the request
   */
  it('should include user email header', (done) => {
    const testDate = '2026-01-15';
    const mockResponse: VvnsWithoutPlansResponse = {
      success: true,
      message: '',
      date: testDate,
      totalApprovedVvns: 0,
      vvnsWithPlans: 0,
      vvnsWithoutPlans: 0,
      missingPlans: []
    };

    service.getVvnsWithoutPlans(testDate).subscribe(() => {
      done();
    });

    const req = httpMock.expectOne(`${OEM_API_URL}/missing-plans?date=${testDate}`);
    expect(req.request.headers.has('X-User-Email')).toBe(true);
    expect(req.request.headers.get('X-User-Email')).toBe('operator@example.com');
    req.flush(mockResponse);
  });

  /**
   * Test 8: Validates that all VVN detail fields are correctly parsed
   * from the API response, including vessel info and timing data
   */
  it('should parse VVN details correctly', (done) => {
    const testDate = '2026-01-15';
    const mockResponse: VvnsWithoutPlansResponse = {
      success: true,
      message: 'Found 1 VVN(s) without Operation Plans',
      date: testDate,
      totalApprovedVvns: 1,
      vvnsWithPlans: 0,
      vvnsWithoutPlans: 1,
      missingPlans: [
        {
          id: 123,
          vesselImoNumber: 'IMO9876543',
          vesselName: 'Cargo Ship Alpha',
          estimatedTimeOfArrival: '2026-01-15T14:30:00Z',
          estimatedTimeOfDeparture: '2026-01-15T22:45:00Z',
          assignedDockCode: 'DOCK-C'
        }
      ]
    };

    service.getVvnsWithoutPlans(testDate).subscribe(response => {
      const vvn = response.missingPlans[0];
      expect(vvn.id).toBe(123);
      expect(vvn.vesselImoNumber).toBe('IMO9876543');
      expect(vvn.vesselName).toBe('Cargo Ship Alpha');
      expect(vvn.estimatedTimeOfArrival).toBe('2026-01-15T14:30:00Z');
      expect(vvn.estimatedTimeOfDeparture).toBe('2026-01-15T22:45:00Z');
      expect(vvn.assignedDockCode).toBe('DOCK-C');
      done();
    });

    const req = httpMock.expectOne(`${OEM_API_URL}/missing-plans?date=${testDate}`);
    req.flush(mockResponse);
  });

  /**
   * Test 9: Validates the statistical calculations in the response
   * Ensures that totalApprovedVvns equals the sum of vvnsWithPlans and vvnsWithoutPlans
   */
  it('should handle response with statistics correctly', (done) => {
    const testDate = '2026-01-15';
    const mockResponse: VvnsWithoutPlansResponse = {
      success: true,
      message: 'Found 1 VVN(s) without Operation Plans',
      date: testDate,
      totalApprovedVvns: 3,
      vvnsWithPlans: 2,
      vvnsWithoutPlans: 1,
      missingPlans: [
        {
          id: 3,
          vesselImoNumber: 'IMO3456789',
          estimatedTimeOfArrival: '2026-01-15T12:00:00Z',
          estimatedTimeOfDeparture: '2026-01-15T20:00:00Z'
        }
      ]
    };

    service.getVvnsWithoutPlans(testDate).subscribe(response => {
      expect(response.totalApprovedVvns).toBe(3);
      expect(response.vvnsWithPlans).toBe(2);
      expect(response.vvnsWithoutPlans).toBe(1);
      expect(response.totalApprovedVvns).toBe(response.vvnsWithPlans + response.vvnsWithoutPlans);
      done();
    });

    const req = httpMock.expectOne(`${OEM_API_URL}/missing-plans?date=${testDate}`);
    req.flush(mockResponse);
  });
});
