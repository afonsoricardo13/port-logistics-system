import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DockDTO {
  code: string;
  nameOrNumber: string;
  location: string;
  length: number; // meters
  depth: number; // meters
  maxDraft: number; // meters
  allowedVesselTypes: string[];
}

export interface DockSearchParams {
  name?: string;
  vesselType?: string;
  location?: string;
}

/**
 * Service for Docks Management
 * 
 * US 2.2.3: Port Authority Officer registers and updates docks
 */
@Injectable({
  providedIn: 'root'
})
export class DockManagementService {
  private readonly apiUrl = `${environment.apiUrl}/api/Docks`;

  constructor(private http: HttpClient) {}

  /**
   * Search and filter docks
   */
  search(params?: DockSearchParams): Observable<DockDTO[]> {
    let httpParams = new HttpParams();
    
    if (params?.name) {
      httpParams = httpParams.set('name', params.name);
    }
    if (params?.vesselType) {
      httpParams = httpParams.set('vesselType', params.vesselType);
    }
    if (params?.location) {
      httpParams = httpParams.set('location', params.location);
    }

    return this.http.get<DockDTO[]>(this.apiUrl, { params: httpParams });
  }

  /**
   * Get all docks
   */
  getAll(): Observable<DockDTO[]> {
    return this.http.get<DockDTO[]>(this.apiUrl);
  }

  /**
   * Get dock by code
   */
  getByCode(code: string): Observable<DockDTO> {
    return this.http.get<DockDTO>(`${this.apiUrl}/${code}`);
  }

  /**
   * Create a new dock
   */
  create(dto: DockDTO): Observable<DockDTO> {
    return this.http.post<DockDTO>(this.apiUrl, dto);
  }

  /**
   * Update an existing dock
   */
  update(code: string, dto: DockDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${code}`, dto);
  }
}
