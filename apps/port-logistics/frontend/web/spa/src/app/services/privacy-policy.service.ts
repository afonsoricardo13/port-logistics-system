import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Interface for Privacy Policy DTO (US 4.5.1)
 */
export interface PrivacyPolicyDto {
  id: number;
  version: number;
  content: string;
  effectiveDate: string;
  createdAt: string;
  isActive: boolean;
  createdBy: string;
  changeNotes?: string;
  notifiedAt?: string;
}

/**
 * Interface for Privacy Policy History Item DTO
 */
export interface PrivacyPolicyHistoryItemDto {
  id: number;
  version: number;
  effectiveDate: string;
  createdAt: string;
  isActive: boolean;
  createdBy: string;
  changeNotes?: string;
  notifiedAt?: string;
  contentPreview: string;
}

/**
 * Interface for Create Privacy Policy DTO
 */
export interface CreatePrivacyPolicyDto {
  content: string;
  effectiveDate: string;
  changeNotes?: string;
  activateImmediately: boolean;
  notifyUsers: boolean;
}

/**
 * Service for Privacy Policy management (US 4.5.1).
 * Handles GDPR-compliant privacy policy operations:
 * - Public access to current policy
 * - Admin management (create, history, activate)
 * - User notifications about policy changes
 * 
 * Implements RGPD Articles 13-14 transparency requirements.
 */
@Injectable({
  providedIn: 'root'
})
export class PrivacyPolicyService {
  private readonly apiUrl = `${environment.apiUrl}/api/privacypolicy`;

  constructor(private http: HttpClient) {}

  /**
   * Gets the currently active privacy policy (public endpoint).
   * Accessible without authentication for GDPR transparency.
   */
  getCurrentPolicy(): Observable<PrivacyPolicyDto> {
    return this.http.get<PrivacyPolicyDto>(`${this.apiUrl}/current`);
  }

  /**
   * Gets a specific privacy policy version by version number.
   * Requires authentication.
   */
  getPolicyByVersion(version: number): Observable<PrivacyPolicyDto> {
    return this.http.get<PrivacyPolicyDto>(`${this.apiUrl}/version/${version}`);
  }

  /**
   * Gets complete history of all policy versions (admin only).
   * Returns list with content preview (not full content).
   */
  getPolicyHistory(): Observable<PrivacyPolicyHistoryItemDto[]> {
    return this.http.get<PrivacyPolicyHistoryItemDto[]>(`${this.apiUrl}/history`);
  }

  /**
   * Creates a new privacy policy version (admin only).
   * Optionally activates immediately and triggers user notifications.
   */
  createPolicy(dto: CreatePrivacyPolicyDto): Observable<PrivacyPolicyDto> {
    return this.http.post<PrivacyPolicyDto>(this.apiUrl, dto, {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' })
    });
  }

  /**
   * Activates a specific privacy policy version (admin only).
   * Deactivates all other versions.
   */
  activatePolicy(id: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Triggers user notifications about policy change (admin only).
   * Implements RGPD Art 13(3) notification requirements.
   */
  notifyUsers(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/notify`, {});
  }
}
