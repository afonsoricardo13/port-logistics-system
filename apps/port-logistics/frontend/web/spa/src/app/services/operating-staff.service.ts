import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface OperatingStaffDTO {
  mecanographicNumber: string;
  name: string;
  email: string;
  phoneNumber: string;
  qualifications: string[];
  operationalWindow: string;
  status: string;
  statusHistory: StatusHistoryEntry[];
}

export interface StatusHistoryEntry {
  status: string; // "Available", "Unavailable", or "Deactivated"
  Status?: string; // Backend sends PascalCase
  timestamp: string;
  Timestamp?: string; // Backend sends PascalCase
  operatorName: string;
  OperatorName?: string; // Backend sends PascalCase
  reason?: string;
  Reason?: string; // Backend sends PascalCase
}

export interface CreateOperatingStaffDTO {
  mecanographicNumber: string;
  name: string;
  email: string;
  phoneNumber: string;
  qualifications: string[];
  operationalWindow: string;
}

export interface UpdateOperatingStaffDTO {
  name: string;
  email: string;
  phoneNumber: string;
  qualifications: string[];
  operationalWindow: string;
}

export interface StatusUpdateRequest {
  isActive: boolean;
  operatorName: string;
  reason?: string;
}

export interface StaffSearchParams {
  status?: string;
  name?: string;
  email?: string;
  qualification?: string;
}

@Injectable({
  providedIn: 'root'
})
export class OperatingStaffService {
  private readonly apiUrl = `${environment.apiUrl}/api/OperatingStaff`;

  constructor(private http: HttpClient) {}

  private normalizeStaff(data: any): OperatingStaffDTO {
    const statusHistory = data.StatusHistory || data.statusHistory || [];
    
    return {
      mecanographicNumber: data.MecanographicNumber || data.mecanographicNumber,
      name: data.Name || data.name,
      email: data.Email || data.email,
      phoneNumber: data.PhoneNumber || data.phoneNumber,
      qualifications: data.Qualifications || data.qualifications || [],
      operationalWindow: data.OperationalWindow || data.operationalWindow,
      status: data.Status || data.status,
      statusHistory: statusHistory.map((entry: any) => ({
        status: entry.Status || entry.status,
        timestamp: entry.Timestamp || entry.timestamp,
        operatorName: entry.OperatorName || entry.operatorName,
        reason: entry.Reason || entry.reason
      }))
    };
  }

  getAll(filters?: StaffSearchParams): Observable<OperatingStaffDTO[]> {
    let params = new HttpParams();
    
    if (filters?.status) {
      params = params.set('status', filters.status);
    }
    if (filters?.name) {
      params = params.set('name', filters.name);
    }
    if (filters?.email) {
      params = params.set('email', filters.email);
    }
    if (filters?.qualification) {
      params = params.set('qualification', filters.qualification);
    }

    return this.http.get<any[]>(this.apiUrl, { params }).pipe(
      map(staffList => staffList.map(staff => this.normalizeStaff(staff)))
    );
  }

  getByMecanographicNumber(mecanographicNumber: string): Observable<OperatingStaffDTO> {
    return this.http.get<any>(`${this.apiUrl}/mecanographic/${mecanographicNumber}`).pipe(
      map(staff => this.normalizeStaff(staff))
    );
  }

  getByStatus(status: string): Observable<OperatingStaffDTO[]> {
    return this.http.get<any[]>(`${this.apiUrl}/status/${status}`).pipe(
      map(staffList => staffList.map(staff => this.normalizeStaff(staff)))
    );
  }

  create(dto: CreateOperatingStaffDTO): Observable<OperatingStaffDTO> {
    return this.http.post<any>(this.apiUrl, dto).pipe(
      map(staff => this.normalizeStaff(staff))
    );
  }

  update(mecanographicNumber: string, dto: UpdateOperatingStaffDTO): Observable<OperatingStaffDTO> {
    return this.http.put<any>(`${this.apiUrl}/${mecanographicNumber}`, dto).pipe(
      map(staff => this.normalizeStaff(staff))
    );
  }

  deactivate(mecanographicNumber: string, operatorName: string, reason?: string): Observable<OperatingStaffDTO> {
    const request: StatusUpdateRequest = {
      isActive: false,
      operatorName,
      reason
    };
    return this.http.patch<any>(`${this.apiUrl}/${mecanographicNumber}/status`, request).pipe(
      map(staff => this.normalizeStaff(staff))
    );
  }

  reactivate(mecanographicNumber: string, operatorName: string, reason?: string): Observable<OperatingStaffDTO> {
    const request: StatusUpdateRequest = {
      isActive: true,
      operatorName,
      reason
    };
    return this.http.patch<any>(`${this.apiUrl}/${mecanographicNumber}/status`, request).pipe(
      map(staff => this.normalizeStaff(staff))
    );
  }
}
