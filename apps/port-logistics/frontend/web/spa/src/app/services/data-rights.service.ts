import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Interface for Data Access Request DTO (US 4.5.3)
 */
export interface CreateDataAccessRequestDto {
  preferredFormat: string; // 'json' or 'pdf'
}

/**
 * Interface for Data Rectification Request DTO (US 4.5.3)
 */
export interface CreateDataRectificationRequestDto {
  fieldsToRectify: { [key: string]: string };
  reason?: string;
}

/**
 * Interface for Data Deletion Request DTO (US 4.5.3)
 */
export interface CreateDataDeletionRequestDto {
  reason?: string;
}

/**
 * Interface for Process Request DTO (Admin - US 4.5.3)
 */
export interface ProcessDataRightsRequestDto {
  requestId: number;
  newStatus: number; // 1=InProgress, 2=Completed, 3=Rejected
  adminResponse?: string;
}

/**
 * Interface for Data Rights Request Response DTO
 */
export interface DataRightsRequestDto {
  id: number;
  userEmail: string;
  requestType: number; // 0=Access, 1=Rectification, 2=Deletion
  requestTypeDisplay: string;
  status: number; // 0=Pending, 1=InProgress, 2=Completed, 3=Rejected
  statusDisplay: string;
  requestDetails?: string;
  proposedChanges?: string;
  exportFormat?: string;
  exportFilePath?: string;
  createdAt: string;
  processedAt?: string;
  processedBy?: string;
  adminResponse?: string;
}

/**
 * Interface for User Data Export DTO
 */
export interface UserDataExportDto {
  email: string;
  name: string;
  role: string;
  createdAt?: string;
  lastLoginAt?: string;
  organization?: string;
  contactInfo?: string;
  exportGeneratedAt: string;
  exportFormat: string;
  dataRightsRequestHistory: DataRightsRequestDto[];
}

/**
 * Service for GDPR Data Rights management (US 4.5.3).
 * Handles user data rights operations:
 * - Right of Access (RGPD Art. 15)
 * - Right to Rectification (RGPD Art. 16)
 * - Right to Erasure (RGPD Art. 17)
 * - Data Portability (RGPD Art. 20)
 */
@Injectable({
  providedIn: 'root'
})
export class DataRightsService {
  private readonly apiUrl = `${environment.apiUrl}/api/datarights`;

  constructor(private http: HttpClient) {}

  // ==================== USER ENDPOINTS ====================

  /**
   * Creates a data access request (right to access personal data).
   * RGPD Article 15.
   */
  createAccessRequest(dto: CreateDataAccessRequestDto): Observable<DataRightsRequestDto> {
    return this.http.post<DataRightsRequestDto>(`${this.apiUrl}/access`, dto);
  }

  /**
   * Creates a data rectification request (right to correct personal data).
   * RGPD Article 16.
   */
  createRectificationRequest(dto: CreateDataRectificationRequestDto): Observable<DataRightsRequestDto> {
    return this.http.post<DataRightsRequestDto>(`${this.apiUrl}/rectification`, dto);
  }

  /**
   * Creates a data deletion request (right to be forgotten).
   * RGPD Article 17.
   */
  createDeletionRequest(dto: CreateDataDeletionRequestDto): Observable<DataRightsRequestDto> {
    return this.http.post<DataRightsRequestDto>(`${this.apiUrl}/deletion`, dto);
  }

  /**
   * Gets all data rights requests for the current user.
   */
  getMyRequests(): Observable<DataRightsRequestDto[]> {
    return this.http.get<DataRightsRequestDto[]>(`${this.apiUrl}/my-requests`);
  }

  /**
   * Gets a specific data rights request by ID.
   */
  getRequestById(id: number): Observable<DataRightsRequestDto> {
    return this.http.get<DataRightsRequestDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Exports the current user's personal data (data portability).
   * Returns JSON file download.
   * RGPD Article 20.
   */
  exportMyData(format: string = 'json'): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export?format=${format}`, {
      responseType: 'blob'
    });
  }

  /**
   * Gets user data for preview before export.
   */
  getExportPreview(): Observable<UserDataExportDto> {
    return this.http.get<UserDataExportDto>(`${this.apiUrl}/export?format=preview`);
  }

  // ==================== ADMIN ENDPOINTS ====================

  /**
   * Gets all data rights requests (admin only).
   */
  getAllRequests(): Observable<DataRightsRequestDto[]> {
    return this.http.get<DataRightsRequestDto[]>(`${this.apiUrl}/admin/all`);
  }

  /**
   * Gets pending data rights requests (admin only).
   */
  getPendingRequests(): Observable<DataRightsRequestDto[]> {
    return this.http.get<DataRightsRequestDto[]>(`${this.apiUrl}/admin/pending`);
  }

  /**
   * Processes a data rights request (admin only).
   */
  processRequest(dto: ProcessDataRightsRequestDto): Observable<DataRightsRequestDto> {
    return this.http.put<DataRightsRequestDto>(`${this.apiUrl}/admin/process`, dto);
  }
}
