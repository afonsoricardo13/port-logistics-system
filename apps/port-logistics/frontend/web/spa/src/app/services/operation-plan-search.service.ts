import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

export interface OperationPlanDetail {
  id: string;
  vvnId: string;
  date: string;
  algorithmUsed: string;
  createdAt: string;
  createdBy: string;
  operations: string[];
  resources: ResourceAssignment[];
  startTime: string | null;
  endTime: string | null;
  duration: number;
  vesselImo: string | null;
  loadingTime: number;
  unloadingTime: number;
  assignedDock: string | null;
  delay: number;
}

export interface ResourceAssignment {
  type: string;
  id: string;
  name: string;
  assignedFrom?: string;
  assignedTo?: string;
}

export interface SearchParams {
  vvnId?: string;
  date?: string;
  startDate?: string;
  endDate?: string;
  sortBy?: string;
  sortOrder?: string;
}

@Injectable({ providedIn: 'root' })
export class OperationPlanSearchService {
  private readonly OEM_API_URL = `${environment.oemApiUrl || 'https://localhost:7001'}/api/OperationPlans`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  searchOperationPlans(params: SearchParams): Observable<OperationPlanDetail[]> {
    const headers = this.getHeaders();
    let httpParams = new HttpParams();

    if (params.vvnId) httpParams = httpParams.set('vvnId', params.vvnId);
    if (params.date) httpParams = httpParams.set('date', params.date);
    if (params.startDate) httpParams = httpParams.set('startDate', params.startDate);
    if (params.endDate) httpParams = httpParams.set('endDate', params.endDate);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortOrder) httpParams = httpParams.set('sortOrder', params.sortOrder);

    return this.http.get<OperationPlanDetail[]>(this.OEM_API_URL, { headers, params: httpParams });
  }

  private getHeaders(): HttpHeaders {
    let userEmail = 'operator@example.com';
    
    try {
      const email = this.authService.getUserEmail();
      if (email) userEmail = email;
    } catch (e) {
      console.log('Using default email for development');
    }
    
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'X-User-Email': userEmail
    });
  }
}
