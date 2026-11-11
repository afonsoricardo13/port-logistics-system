import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

export interface IncidentDTO {
  id: string;
  incidentTypeCode: string;
  incidentTypeName: string;
  startTime: string;
  endTime?: string;
  severity: 'Minor' | 'Major' | 'Critical';
  description: string;
  affectedVVEIds: string[];
  status: 'Active' | 'Resolved';
  createdBy: string;
  createdAt: string;
  resolvedAt?: string;
  durationMinutes?: number;
  isActive: boolean;
}

export interface CreateIncidentDTO {
  incidentTypeCode: string;
  startTime: string;
  severity: string;
  description: string;
  createdBy: string;
  affectedVVEIds?: string[];
}

export interface UpdateIncidentDTO {
  incidentTypeCode?: string;
  endTime?: string;
  severity?: string;
  description?: string;
  status?: string;
  affectedVVEIds?: string[];
}

export interface IncidentFilterParams {
  status?: string;
  severity?: string;
  startDate?: string;
  endDate?: string;
  activeOnly?: boolean;
  vesselVisitId?: string;
}

@Injectable({ providedIn: 'root' })
export class IncidentsService {
  // OEM API URL - Operations Execution Management
  private readonly API_URL = `${environment.oemApiUrl || 'https://localhost:7001'}/api/Incidents`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  private getHeaders(): HttpHeaders {
    const userEmail = this.authService.getUserEmail();
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'X-User-Email': userEmail || 'system@portlogistics.com'
    });
  }

  /**
   * Get all incidents with optional filters
   */
  getAll(filters?: IncidentFilterParams): Observable<IncidentDTO[]> {
    let params = new HttpParams();
    
    if (filters) {
      if (filters.status) params = params.set('status', filters.status);
      if (filters.severity) params = params.set('severity', filters.severity);
      if (filters.startDate) params = params.set('startDate', filters.startDate);
      if (filters.endDate) params = params.set('endDate', filters.endDate);
      if (filters.activeOnly !== undefined) params = params.set('activeOnly', filters.activeOnly.toString());
      if (filters.vesselVisitId) params = params.set('vesselVisitId', filters.vesselVisitId);
    }

    return this.http.get<IncidentDTO[]>(this.API_URL, {
      headers: this.getHeaders(),
      params
    });
  }

  /**
   * Get incident by ID
   */
  getById(id: string): Observable<IncidentDTO> {
    return this.http.get<IncidentDTO>(`${this.API_URL}/${id}`, {
      headers: this.getHeaders()
    });
  }

  /**
   * Create a new incident
   */
  create(dto: CreateIncidentDTO): Observable<IncidentDTO> {
    return this.http.post<IncidentDTO>(this.API_URL, dto, {
      headers: this.getHeaders()
    });
  }

  /**
   * Update an existing incident
   */
  update(id: string, dto: UpdateIncidentDTO): Observable<IncidentDTO> {
    return this.http.put<IncidentDTO>(`${this.API_URL}/${id}`, dto, {
      headers: this.getHeaders()
    });
  }

  /**
   * Mark incident as resolved
   */
  resolve(id: string, endTime?: string): Observable<IncidentDTO> {
    const body = endTime ? { endTime } : {};
    return this.http.patch<IncidentDTO>(`${this.API_URL}/${id}/resolve`, body, {
      headers: this.getHeaders()
    });
  }

  /**
   * Update affected VVEs
   */
  updateAffectedVVEs(id: string, vveIds: string[]): Observable<IncidentDTO> {
    return this.http.put<IncidentDTO>(`${this.API_URL}/${id}/affected-vves`, 
      { vveIds }, 
      { headers: this.getHeaders() }
    );
  }

  /**
   * Delete an incident
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`, {
      headers: this.getHeaders()
    });
  }

  /**
   * Get active incidents for a specific VVE
   */
  getActiveIncidentsForVVE(vveId: string): Observable<IncidentDTO[]> {
    return this.http.get<IncidentDTO[]>(`${this.API_URL}/vessel-visit/${vveId}`, {
      headers: this.getHeaders()
    });
  }
}
