import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

export interface ComplementaryTaskDTO {
  id: string;
  categoryCode: string;
  categoryName: string;
  vveId: string;
  vesselName: string;
  responsibleTeam: string;
  startTime: string;
  endTime?: string;
  status: 'Ongoing' | 'Completed';
  impactType: 'Parallel' | 'Blocking';
  notes?: string;
  createdAt: string;
  updatedAt?: string;
  isOngoing: boolean;
  durationMinutes?: number;
}

export interface CreateComplementaryTaskDTO {
  categoryCode: string;
  vveId: string;
  responsibleTeam: string;
  startTime: string;
  impactType: string; // "Parallel" or "Blocking"
  notes?: string;
}

export interface UpdateComplementaryTaskDTO {
  categoryCode?: string;
  responsibleTeam?: string;
  startTime?: string;
  endTime?: string;
  status?: string; // "Ongoing" or "Completed"
  impactType?: string; // "Parallel" or "Blocking"
  notes?: string;
}

export interface ComplementaryTaskFilterParams {
  status?: string;
  vveId?: string;
  startDate?: string;
  endDate?: string;
  ongoingOnly?: boolean;
  impactType?: string;
}

@Injectable({ providedIn: 'root' })
export class ComplementaryTasksService {
  // OEM API URL - Operations Execution Management
  private readonly API_URL = `${environment.oemApiUrl || 'https://localhost:7001'}/api/ComplementaryTasks`;

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
   * Get all complementary tasks with optional filters
   */
  getAll(filters?: ComplementaryTaskFilterParams): Observable<ComplementaryTaskDTO[]> {
    let params = new HttpParams();
    
    if (filters) {
      if (filters.status) params = params.set('status', filters.status);
      if (filters.vveId) params = params.set('vveId', filters.vveId);
      if (filters.startDate) params = params.set('startDate', filters.startDate);
      if (filters.endDate) params = params.set('endDate', filters.endDate);
      if (filters.ongoingOnly !== undefined) params = params.set('ongoingOnly', filters.ongoingOnly.toString());
      if (filters.impactType) params = params.set('impactType', filters.impactType);
    }

    return this.http.get<ComplementaryTaskDTO[]>(this.API_URL, {
      headers: this.getHeaders(),
      params
    });
  }

  /**
   * Get ongoing tasks that are currently impacting operations
   */
  getOngoing(): Observable<ComplementaryTaskDTO[]> {
    return this.http.get<ComplementaryTaskDTO[]>(`${this.API_URL}/ongoing`, {
      headers: this.getHeaders()
    });
  }

  /**
   * Get complementary task by ID
   */
  getById(id: string): Observable<ComplementaryTaskDTO> {
    return this.http.get<ComplementaryTaskDTO>(`${this.API_URL}/${id}`, {
      headers: this.getHeaders()
    });
  }

  /**
   * Get tasks by VVE ID
   */
  getByVveId(vveId: string): Observable<ComplementaryTaskDTO[]> {
    return this.http.get<ComplementaryTaskDTO[]>(`${this.API_URL}/vve/${vveId}`, {
      headers: this.getHeaders()
    });
  }

  /**
   * Create a new complementary task
   */
  create(dto: CreateComplementaryTaskDTO): Observable<ComplementaryTaskDTO> {
    return this.http.post<ComplementaryTaskDTO>(this.API_URL, dto, {
      headers: this.getHeaders()
    });
  }

  /**
   * Update an existing complementary task
   */
  update(id: string, dto: UpdateComplementaryTaskDTO): Observable<ComplementaryTaskDTO> {
    return this.http.put<ComplementaryTaskDTO>(`${this.API_URL}/${id}`, dto, {
      headers: this.getHeaders()
    });
  }

  /**
   * Mark task as completed
   */
  complete(id: string, endTime?: string): Observable<ComplementaryTaskDTO> {
    const dto: UpdateComplementaryTaskDTO = {
      status: 'Completed',
      endTime: endTime || new Date().toISOString()
    };
    return this.update(id, dto);
  }

  /**
   * Delete a complementary task
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`, {
      headers: this.getHeaders()
    });
  }
}
