import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

/**
 * US 4.1.2 - Operation Planning Service
 * Service to interact with OEM API for saving operation plans
 */

export interface OperationPlanSummary {
  id: string;
  vvnId: string;
  date: string;
  algorithmUsed: string;
  createdAt: string;
  createdBy: string;
  operationsCount: number;
}

export interface GenerateOperationPlansRequest {
  date: string;  // ISO 8601 format
  algorithm: string;  // "optimal", "heuristic", or "multi_crane"
}

export interface GenerateOperationPlansResponse {
  success: boolean;
  message: string;
  date: string;
  plansGenerated: number;
  plans: OperationPlanSummary[];
  error?: string;
}

/**
 * US 4.1.5: DTO for VVN without Operation Plan
 */
export interface VvnWithoutPlan {
  id: number;
  vesselImoNumber: string;
  vesselName?: string;
  estimatedTimeOfArrival?: string;
  estimatedTimeOfDeparture?: string;
  assignedDockCode?: string;
}

/**
 * US 4.1.5: Response for VVNs without Operation Plans
 */
export interface VvnsWithoutPlansResponse {
  success: boolean;
  message: string;
  date: string;
  totalApprovedVvns: number;
  vvnsWithPlans: number;
  vvnsWithoutPlans: number;
  missingPlans: VvnWithoutPlan[];
}

/**
 * US 4.1.4: Resource assignment for operation plan updates
 */
export interface ResourceAssignment {
  type: string;  // "crane", "staff", "dock"
  id: string;
  name?: string;  // Populated by backend from database
  assignedFrom?: string;
  assignedTo?: string;
}

/**
 * US 4.1.4: Operation detail
 */
export interface OperationDetail {
  type: string;  // "loading", "unloading"
  startTime: string;
  endTime: string;
  duration: number;
  assignedCrane?: string;
  assignedDock?: string;
}

/**
 * US 4.1.4: Request to update an operation plan manually
 */
export interface UpdateOperationPlanRequest {
  reasonForChange: string;
  craneAssignments?: ResourceAssignment[];
  staffAssignments?: ResourceAssignment[];
  assignedDock?: string;
  startTime?: string;
  endTime?: string;
  operations?: OperationDetail[];
}

/**
 * US 4.1.4: Change log entry
 */
export interface ChangeLogEntry {
  timestamp: string;
  author: string;
  reason: string;
  changes: string[];
}

/**
 * US 4.1.4: Resource conflict
 */
export interface ResourceConflict {
  resourceType: string;
  resourceId: string;
  conflictDescription: string;
  conflictingVvnIds: string[];
  conflictStartTime: string;
  conflictEndTime: string;
}

/**
 * US 4.1.4: VVN inconsistency
 */
export interface VvnInconsistency {
  vvnId: string;
  inconsistencyType: string;
  description: string;
  severity: string;  // "warning", "error"
}

/**
 * US 4.1.4: Operation plan detailed response
 */
export interface OperationPlanDetailed {
  id: string;
  vvnId: string;
  date: string;
  algorithmUsed: string;
  createdAt: string;
  createdBy: string;
  operations: string[];
  resources: ResourceAssignment[];
  startTime?: string;
  endTime?: string;
  duration: number;
  vesselImo?: string;
  loadingTime: number;
  unloadingTime: number;
  assignedDock?: string;
  delay: number;
  updatedBy?: string;
  updatedAt?: string;
  changeLogs?: ChangeLogEntry[];
}

/**
 * US 4.1.4: Response for operation plan update
 */
export interface UpdateOperationPlanResponse {
  success: boolean;
  message: string;
  updatedPlan?: OperationPlanDetailed;
  warnings: string[];
  resourceConflicts: ResourceConflict[];
  vvnInconsistencies: VvnInconsistency[];
}

/**
 * US 4.1.6 - Result of resource usage aggregation
 */
export interface ResourceUsageResult {
  success: boolean;
  message: string;
  resourceId: string;
  resourceType: string;
  startDate: string;
  endDate: string;
  totalUsage: number;
  details: Array<{
    date: string;
    usage: number;
  }>;
}

@Injectable({ providedIn: 'root' })
export class OperationPlanningService {
  // OEM API URL - Operations Execution Management
  private readonly OEM_API_URL = `${environment.oemApiUrl || 'https://localhost:7001'}/api/OperationPlans`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  /**
   * US 4.1.2 - Generate and save operation plans to OEM database
   * Calls the OEM API which in turn calls the scheduling service
   */
  generateAndSaveOperationPlans(date: string, algorithm: string): Observable<GenerateOperationPlansResponse> {
    const headers = this.getHeaders();

    const request: GenerateOperationPlansRequest = {
      date: date,
      algorithm: algorithm
    };

    return this.http.post<GenerateOperationPlansResponse>(
      `${this.OEM_API_URL}/generate`,
      request,
      { headers }
    );
  }

  /**
   * Get operation plans by date
   */
  getOperationPlansByDate(date: string): Observable<OperationPlanSummary[]> {
    const headers = this.getHeaders();
    return this.http.get<OperationPlanSummary[]>(
      `${this.OEM_API_URL}?date=${date}`,
      { headers }
    );
  }

  /**
   * US 4.1.9 - Get operation plans by VVN ID
   */
  getOperationPlansByVvnId(vvnId: string): Observable<OperationPlanDetailed[]> {
    const headers = this.getHeaders();
    const params = new URLSearchParams({ vvnId }).toString();
    return this.http.get<OperationPlanDetailed[]>(
      `${this.OEM_API_URL}?${params}`,
      { headers }
    );
  }

  /**
   * US 4.1.5 - Get VVNs without Operation Plans for a given date
   */
  getVvnsWithoutPlans(date: string): Observable<VvnsWithoutPlansResponse> {
    const headers = this.getHeaders();
    return this.http.get<VvnsWithoutPlansResponse>(
      `${this.OEM_API_URL}/missing-plans?date=${date}`,
      { headers }
    );
  }

  /**
   * US 4.1.5 - Regenerate all Operation Plans for a given day
   * WARNING: This will DELETE existing plans and create new ones
   */
  regenerateOperationPlans(date: string, algorithm: string): Observable<GenerateOperationPlansResponse> {
    const headers = this.getHeaders();

    const request: GenerateOperationPlansRequest = {
      date: date,
      algorithm: algorithm
    };

    return this.http.post<GenerateOperationPlansResponse>(
      `${this.OEM_API_URL}/regenerate`,
      request,
      { headers }
    );
  }



  /**
   * US 4.1.4 - Manually update an operation plan
   * Allows logistics operators to make last-minute adjustments
   */
  updateOperationPlan(planId: string, request: UpdateOperationPlanRequest): Observable<UpdateOperationPlanResponse> {
    const headers = this.getHeaders();
    return this.http.patch<UpdateOperationPlanResponse>(
      `${this.OEM_API_URL}/${planId}`,
      request,
      { headers }
    );
  }

  /**
   * US 4.1.4 - Get operation plan by ID with full details
   */
  getOperationPlanById(planId: string): Observable<OperationPlanDetailed> {
    const headers = this.getHeaders();
    return this.http.get<OperationPlanDetailed>(
      `${this.OEM_API_URL}/${planId}`,
      { headers }
    );
  }

  /**
   * US 4.1.4 - Clear change history logs for an operation plan
   */
  clearChangeLogs(planId: string): Observable<{ success: boolean; message: string }> {
    const headers = this.getHeaders();
    return this.http.delete<{ success: boolean; message: string }>(
      `${this.OEM_API_URL}/${planId}/change-logs`,
      { headers }
    );
  }


  /**
   * US 4.1.6 - Get aggregated resource usage for a resource in a period
   */
  getResourceUsage(resourceType: string, resourceId: string, startDate: string, endDate: string): Observable<ResourceUsageResult> {
    const headers = this.getHeaders();
    const params = new URLSearchParams({ resourceType, resourceId, startDate, endDate }).toString();
    return this.http.get<ResourceUsageResult>(`${this.OEM_API_URL}/resource-usage?${params}`, { headers });
  }

  /**
   * Get headers with user authentication
   */
  private getHeaders(): HttpHeaders {
    let userEmail = 'operator@example.com'; // Default for development

    try {
      const email = this.authService.getUserEmail();
      if (email) {
        userEmail = email;
      }
    } catch (e) {
      // Ignore auth errors in development
      console.log('Using default email for development');
    }

    return new HttpHeaders({
      'Content-Type': 'application/json',
      'X-User-Email': userEmail
    });
  }
}
