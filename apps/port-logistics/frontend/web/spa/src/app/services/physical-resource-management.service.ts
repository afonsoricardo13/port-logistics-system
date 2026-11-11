import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PhysicalResourceDTO {
  code: string;
  description: string;
  type: string; // Crane, Truck, etc.
  capacity: number;
  assignedArea?: string;
  status?: string; // 'Active' | 'Inactive' | 'UnderMaintenance'
  setupTimeMinutes?: number;
  qualificationRequirementIds?: string[];
  operationalWindow?: string;
  capacityPerHour?: number;
  averageSpeedKmH?: number;
  changedAt?: string;
  changedBy?: string;
  operationType?: string;
}

export interface PhysicalResourceSearchParams {
  code?: string;
  description?: string;
  type?: string;
  status?: string;
}

/**
 * Service for Physical Resources Management
 * 
 * US 2.2.12: Logistics Operator registers and manages physical resources
 */
@Injectable({
  providedIn: 'root'
})
export class PhysicalResourceManagementService {
  private readonly apiUrl = `${environment.apiUrl}/api/PhysicalResources`;

  constructor(private http: HttpClient) {}

  /**
   * Search and filter physical resources
   */
  search(params?: PhysicalResourceSearchParams): Observable<PhysicalResourceDTO[]> {
    let httpParams = new HttpParams();
    
    if (params?.code) {
      httpParams = httpParams.set('code', params.code);
    }
    if (params?.description) {
      httpParams = httpParams.set('description', params.description);
    }
    if (params?.type) {
      httpParams = httpParams.set('type', params.type);
    }
    if (params?.status) {
      httpParams = httpParams.set('status', params.status);
    }

    return this.http.get<PhysicalResourceDTO[]>(this.apiUrl, { params: httpParams });
  }

  /**
   * Get all physical resources
   */
  getAll(): Observable<PhysicalResourceDTO[]> {
    return this.http.get<PhysicalResourceDTO[]>(this.apiUrl);
  }

  /**
   * Get physical resource by code
   */
  getByCode(code: string): Observable<PhysicalResourceDTO> {
    return this.http.get<PhysicalResourceDTO>(`${this.apiUrl}/${code}`);
  }

  /**
   * Create a new physical resource
   */
  create(dto: PhysicalResourceDTO): Observable<PhysicalResourceDTO> {
    return this.http.post<PhysicalResourceDTO>(this.apiUrl, dto);
  }

  /**
   * Update an existing physical resource
   */
  update(code: string, dto: PhysicalResourceDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${code}`, dto);
  }

  /**
   * Deactivate a physical resource (soft delete - preserves data)
   */
  deactivate(code: string, changedBy?: string): Observable<void> {
    let httpParams = new HttpParams().set('status', 'inactive');
    if (changedBy) {
      httpParams = httpParams.set('changedBy', changedBy);
    }
    return this.http.patch<void>(`${this.apiUrl}/${code}`, null, { params: httpParams });
  }

  /**
   * Reactivate a physical resource
   */
  reactivate(code: string, changedBy?: string): Observable<void> {
    let httpParams = new HttpParams().set('status', 'active');
    if (changedBy) {
      httpParams = httpParams.set('changedBy', changedBy);
    }
    return this.http.patch<void>(`${this.apiUrl}/${code}`, null, { params: httpParams });
  }

  /**
   * Get status badge CSS class
   */
  getStatusClass(status?: string): string {
    switch (status?.toLowerCase()) {
      case 'active':
        return 'bg-success';
      case 'inactive':
        return 'bg-secondary';
      case 'undermaintenance':
        return 'bg-warning';
      default:
        return 'bg-secondary';
    }
  }

  /**
   * Get friendly status label
   */
  getStatusLabel(status?: string): string {
    switch (status?.toLowerCase()) {
      case 'active':
        return 'Active';
      case 'inactive':
        return 'Inactive';
      case 'undermaintenance':
        return 'Under Maintenance';
      default:
        return status || 'Unknown';
    }
  }
}
