import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Qualification DTO (full data)
 * 
 * US 2.2.13: Each qualification has a unique code (qualificationName) 
 * and descriptive name/details
 */
export interface QualificationDTO {
  qualificationName: string; // Unique identifier (e.g., "STS Crane Operator")
  requiredNumber: number;
}

/**
 * Create Qualification DTO
 */
export interface CreateQualificationDTO {
  qualificationName: string;
  requiredNumber: number;
}

/**
 * Update Qualification DTO (only requiredNumber can be updated)
 */
export interface UpdateQualificationDTO {
  requiredNumber: number;
}

/**
 * Service for Qualifications Management
 * 
 * US 2.2.13: Register and manage qualifications so that staff members 
 * and resources can be consistently associated with correct skills
 */
@Injectable({
  providedIn: 'root'
})
export class QualificationService {
  private readonly apiUrl = `${environment.apiUrl}/api/Qualification`;

  constructor(private http: HttpClient) {}

  /**
   * Get all qualifications with optional name filter
   * 
   * @param namePattern Optional name pattern for filtering
   * @returns Observable of qualifications array
   */
  getAll(namePattern?: string): Observable<QualificationDTO[]> {
    let params = new HttpParams();
    if (namePattern) {
      params = params.set('namePattern', namePattern);
    }
    return this.http.get<QualificationDTO[]>(this.apiUrl, { params });
  }

  /**
   * Get qualification by name (unique identifier)
   * 
   * @param qualificationName Qualification name
   * @returns Observable of qualification
   */
  getByName(qualificationName: string): Observable<QualificationDTO> {
    return this.http.get<QualificationDTO>(
      `${this.apiUrl}/${encodeURIComponent(qualificationName)}`
    );
  }

  /**
   * Create a new qualification
   * 
   * @param dto Qualification data
   * @returns Observable of created qualification
   */
  create(dto: CreateQualificationDTO): Observable<QualificationDTO> {
    return this.http.post<QualificationDTO>(this.apiUrl, dto);
  }

  /**
   * Update an existing qualification
   * 
   * @param qualificationName Qualification name (immutable identifier)
   * @param dto Updated qualification data
   * @returns Observable of updated qualification
   */
  update(qualificationName: string, dto: UpdateQualificationDTO): Observable<QualificationDTO> {
    return this.http.put<QualificationDTO>(
      `${this.apiUrl}/${encodeURIComponent(qualificationName)}`,
      dto
    );
  }

  /**
   * Check if a qualification exists
   * 
   * @param qualificationName Qualification name
   * @returns Observable of existence check result
   */
  exists(qualificationName: string): Observable<{ qualificationName: string; exists: boolean }> {
    return this.http.get<{ qualificationName: string; exists: boolean }>(
      `${this.apiUrl}/${encodeURIComponent(qualificationName)}/exists`
    );
  }

  /**
   * Search qualifications by name pattern
   * 
   * @param namePattern Name pattern for search
   * @returns Observable of matching qualifications
   */
  search(namePattern: string): Observable<QualificationDTO[]> {
    return this.getAll(namePattern);
  }
}
