import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Vessel Type DTO
 * 
 * US 2.2.1: Vessel types must include attributes such as name, description, 
 * capacity, and operational constraints (max rows, bays, tiers)
 */
export interface VesselTypeDTO {
  name: string;
  description: string;
  capacity: number;
  maxRows: number;
  maxBays: number;
  maxTiers: number;
}

/**
 * Service for Vessel Type Management
 * 
 * US 2.2.1: Create and update vessel types for consistent vessel classification
 * and operational constraints definition
 */
@Injectable({
  providedIn: 'root'
})
export class VesselTypeManagementService {
  private readonly apiUrl = `${environment.apiUrl}/api/VesselTypes`;

  constructor(private http: HttpClient) {}

  /**
   * Get all vessel types with optional search filters
   * 
   * @param name Optional name filter
   * @param description Optional description filter
   * @returns Observable of vessel type array
   */
  getAll(name?: string, description?: string): Observable<VesselTypeDTO[]> {
    let params = new HttpParams();
    if (name) {
      params = params.set('name', name);
    }
    if (description) {
      params = params.set('description', description);
    }
    return this.http.get<VesselTypeDTO[]>(this.apiUrl, { params });
  }

  /**
   * Get vessel type by name
   * 
   * @param name Vessel type name (unique identifier)
   * @returns Observable of vessel type
   */
  getByName(name: string): Observable<VesselTypeDTO> {
    return this.http.get<VesselTypeDTO>(`${this.apiUrl}/${encodeURIComponent(name)}`);
  }

  /**
   * Create a new vessel type
   * 
   * @param dto Vessel type data
   * @returns Observable of created vessel type
   */
  create(dto: VesselTypeDTO): Observable<VesselTypeDTO> {
    return this.http.post<VesselTypeDTO>(this.apiUrl, dto);
  }

  /**
   * Update an existing vessel type
   * 
   * @param name Original vessel type name
   * @param dto Updated vessel type data
   * @returns Observable of void
   */
  update(name: string, dto: VesselTypeDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${encodeURIComponent(name)}`, dto);
  }

  /**
   * Search vessel types by name and/or description
   * 
   * @param name Optional name search term
   * @param description Optional description search term
   * @returns Observable of matching vessel types
   */
  search(name?: string, description?: string): Observable<VesselTypeDTO[]> {
    return this.getAll(name, description);
  }
}
