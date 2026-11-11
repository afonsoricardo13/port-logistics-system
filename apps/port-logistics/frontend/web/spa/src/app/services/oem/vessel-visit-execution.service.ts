import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth.service';

export interface VesselVisitExecutionDTO {
  id?: string;
  vvnId: string;
  actualArrivalTime?: string;
  actualBerthTime?: string;
  dockUsed?: string;
  executedOperations?: string;
  actualUnberthTime?: string;
  actualDepartureTime?: string;
  status: 'InProgress' | 'Completed';
  createdBy: string;
  createdAt?: string;
  updatedBy?: string;
  updatedAt?: string;
  notes?: string;
}

export interface CreateVveDTO {
  vvnId: string;
  actualArrivalTime: string;
  notes?: string;
}

export interface UpdateVveBerthingDTO {
  actualBerthTime: string;
  dockUsed: string;
}

export interface CompleteVveDTO {
  actualUnberthTime: string;
  actualDepartureTime: string;
  notes?: string;
}

/**
 * US 4.1.10: Search result DTO with execution metrics
 */
export interface VesselVisitExecutionSearchResultDTO {
  id?: string;
  vvnId: string;
  vesselImo?: string;
  vesselName?: string;
  actualArrivalTime?: string;
  actualBerthTime?: string;
  dockUsed?: string;
  actualUnberthTime?: string;
  actualDepartureTime?: string;
  status: string;
  expectedArrivalTime?: string;
  expectedDepartureTime?: string;
  
  // Execution Metrics
  totalTurnaroundTimeHours?: number;
  berthOccupancyTimeHours?: number;
  waitingTimeForBerthingHours?: number;
  arrivalDelayHours?: number;
  departureDelayHours?: number;
  executedOperationsCount?: number;
  
  createdAt?: string;
  updatedAt?: string;
  notes?: string;
}

@Injectable({
  providedIn: 'root'
})
export class VesselVisitExecutionService {
  private readonly apiUrl = `${environment.oemApiUrl}/api/VesselVisitExecutions`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  /**
   * US 4.1.7: Create a new Vessel Visit Execution
   */
  create(vve: CreateVveDTO): Observable<VesselVisitExecutionDTO> {
    const userEmail = this.authService.getUserEmail();
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'X-User-Email': userEmail || 'unknown@example.com'
    });

    return this.http.post<VesselVisitExecutionDTO>(this.apiUrl, vve, { headers });
  }

  /**
   * US 4.1.8: Update VVE with actual berth time and dock
   */
  updateBerthing(id: string, dto: UpdateVveBerthingDTO): Observable<VesselVisitExecutionDTO> {
    const userEmail = this.authService.getUserEmail();
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'X-User-Email': userEmail || 'unknown@example.com'
    });

    return this.http.patch<VesselVisitExecutionDTO>(`${this.apiUrl}/${id}/berthing`, dto, { headers });
  }

  /**
   * Get all Vessel Visit Executions
   */
  getAll(): Observable<VesselVisitExecutionDTO[]> {
    const userEmail = this.authService.getUserEmail();
    const headers = new HttpHeaders({
      'X-User-Email': userEmail || 'unknown@example.com'
    });

    return this.http.get<VesselVisitExecutionDTO[]>(this.apiUrl, { headers });
  }

  /**
   * US 4.1.9: Get VVE by ID
   */
  getById(id: string): Observable<VesselVisitExecutionDTO> {
    const userEmail = this.authService.getUserEmail();
    const headers = new HttpHeaders({
      'X-User-Email': userEmail || 'unknown@example.com'
    });

    return this.http.get<VesselVisitExecutionDTO>(`${this.apiUrl}/${id}`, { headers });
  }

  /**
   * US 4.1.9: Update VVE
   */
  update(id: string, vve: VesselVisitExecutionDTO): Observable<VesselVisitExecutionDTO> {
    const userEmail = this.authService.getUserEmail();
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'X-User-Email': userEmail || 'unknown@example.com'
    });

    return this.http.put<VesselVisitExecutionDTO>(`${this.apiUrl}/${id}`, vve, { headers });
  }

  /**
   * US 4.1.11: Complete a Vessel Visit Execution
   */
  complete(id: string, dto: CompleteVveDTO): Observable<VesselVisitExecutionDTO> {
    const userEmail = this.authService.getUserEmail();
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'X-User-Email': userEmail || 'unknown@example.com'
    });

    return this.http.post<VesselVisitExecutionDTO>(`${this.apiUrl}/${id}/complete`, dto, { headers });
  }

  /**
   * US 4.1.10: Search VVEs with execution metrics
   */
  searchWithMetrics(
    startDate?: string,
    endDate?: string,
    vesselImo?: string,
    status?: string
  ): Observable<VesselVisitExecutionSearchResultDTO[]> {
    const userEmail = this.authService.getUserEmail();
    const headers = new HttpHeaders({
      'X-User-Email': userEmail || 'unknown@example.com'
    });

    let params: any = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;
    if (vesselImo) params.vesselImo = vesselImo;
    if (status) params.status = status;

    return this.http.get<VesselVisitExecutionSearchResultDTO[]>(`${this.apiUrl}/search`, { headers, params });
  }
}
