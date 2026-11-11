import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RepresentativeDTO {
  id?: number;
  name: string;
  email: string;
  citizenId: string;
  nationality: string;
  phoneNumber: string;
  organizationId: number;
  organizationName?: string;
  isActive?: boolean;
  createdAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class RepresentativeService {
  private apiUrl = `${environment.apiUrl}/api/Representatives`;

  constructor(private http: HttpClient) {}

  /**
   * Get all representatives
   */
  getAll(): Observable<RepresentativeDTO[]> {
    return this.http.get<RepresentativeDTO[]>(this.apiUrl);
  }

  /**
   * Get representative by ID
   */
  getById(id: number): Observable<RepresentativeDTO> {
    return this.http.get<RepresentativeDTO>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get representatives by organization
   */
  getByOrganization(organizationId: number): Observable<RepresentativeDTO[]> {
    return this.http.get<RepresentativeDTO[]>(`${this.apiUrl}/organization/${organizationId}`);
  }

  /**
   * US 2.2.6: Create new representative
   */
  create(dto: Omit<RepresentativeDTO, 'Id' | 'OrganizationName' | 'IsActive' | 'CreatedAt'>): Observable<RepresentativeDTO> {
    return this.http.post<RepresentativeDTO>(`${this.apiUrl}/organization/${dto.organizationId}`, dto);
  }

  /**
   * US 2.2.6: Update representative
   */
  update(id: number, dto: Partial<RepresentativeDTO>): Observable<RepresentativeDTO> {
    return this.http.put<RepresentativeDTO>(`${this.apiUrl}/${id}`, dto);
  }

  /**
   * US 2.2.6: Deactivate representative
   */
  deactivate(id: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Reactivate representative
   */
  reactivate(id: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/reactivate`, {});
  }
}
