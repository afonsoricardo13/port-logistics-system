import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth.service';

export interface ExecutedOperationDTO {
  id: string;
  vveId: string;
  operationPlanId?: string;
  plannedOperationIndex: number;
  type: string;
  status: 'Started' | 'Completed' | 'Delayed';
  plannedStartTime?: string;
  plannedEndTime?: string;
  actualStartTime?: string;
  actualEndTime?: string;
  actualResources?: ResourceUsageDTO[];
  delayMinutes?: number;
  updatedBy: string;
  updatedAt: string;
  createdAt: string;
  notes?: string;
}

export interface ResourceUsageDTO {
  resourceType: string;
  resourceId: string;
  quantity: number;
}

export interface CreateExecutedOperationDTO {
  vveId: string;
  operationPlanId?: string;
  plannedOperationIndex: number;
  type: string;
  plannedStartTime?: string;
  plannedEndTime?: string;
  actualStartTime?: string;
  actualResources?: ResourceUsageDTO[];
  notes?: string;
}

export interface UpdateExecutedOperationDTO {
  actualStartTime?: string;
  actualEndTime?: string;
  actualResources?: ResourceUsageDTO[];
  status?: 'Started' | 'Completed' | 'Delayed';
  notes?: string;
}

export interface InitializeFromPlanRequest {
  vveId: string;
  operationPlanId: string;
}

export interface InitializeOperationsFromPlanResponseDTO {
  success: boolean;
  message: string;
  operationsInitialized: number;
  operations: ExecutedOperationDTO[];
}

/**
 * US 4.1.9: Service for managing executed operations within vessel visit executions
 */
@Injectable({
  providedIn: 'root'
})
export class ExecutedOperationService {
  private readonly API_URL = `${environment.oemApiUrl || 'https://localhost:7001'}/api/ExecutedOperations`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  private getHeaders(): HttpHeaders {
    const userEmail = this.authService.getUserEmail();
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'X-User-Email': userEmail || 'operator@example.com'
    });
  }

  /**
   * Get all executed operations for a specific VVE
   */
  getByVveId(vveId: string): Observable<ExecutedOperationDTO[]> {
    return this.http.get<ExecutedOperationDTO[]>(
      `${this.API_URL}/vve/${vveId}`,
      { headers: this.getHeaders() }
    );
  }

  /**
   * Get executed operation by ID
   */
  getById(id: string): Observable<ExecutedOperationDTO> {
    return this.http.get<ExecutedOperationDTO>(
      `${this.API_URL}/${id}`,
      { headers: this.getHeaders() }
    );
  }

  /**
   * Initialize executed operations from an operation plan
   * This creates ExecutedOperation records from the planned operations
   */
  initializeFromPlan(request: InitializeFromPlanRequest): Observable<InitializeOperationsFromPlanResponseDTO> {
    return this.http.post<InitializeOperationsFromPlanResponseDTO>(
      `${this.API_URL}/initialize-from-plan`,
      request,
      { headers: this.getHeaders() }
    );
  }

  /**
   * Create a new executed operation manually
   */
  create(dto: CreateExecutedOperationDTO): Observable<ExecutedOperationDTO> {
    return this.http.post<ExecutedOperationDTO>(
      this.API_URL,
      dto,
      { headers: this.getHeaders() }
    );
  }

  /**
   * Update an executed operation with actual times and resources
   */
  update(id: string, dto: UpdateExecutedOperationDTO): Observable<ExecutedOperationDTO> {
    return this.http.put<ExecutedOperationDTO>(
      `${this.API_URL}/${id}`,
      dto,
      { headers: this.getHeaders() }
    );
  }

  /**
   * Delete an executed operation
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.API_URL}/${id}`,
      { headers: this.getHeaders() }
    );
  }
}
