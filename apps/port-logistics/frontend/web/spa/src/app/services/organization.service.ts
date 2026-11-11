import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AddressDTO {
  street: string;
  postalCode: string;
  city: string;
}

export interface OrganizationDTO {
  id?: number;
  identifier: string;
  legalName: string;
  alternativeName: string;
  taxNumber: string;
  address: AddressDTO;
  representatives?: any[];
}

export interface CreateOrganizationDTO {
  identifier: string;
  legalName: string;
  alternativeName: string;
  taxNumber: string;
  address: AddressDTO;
  initialRepresentative: {
    name: string;
    email: string;
    citizenId: string;
    nationality: string;
    phoneNumber: string;
  };
}

@Injectable({
  providedIn: 'root'
})
export class OrganizationService {
  private apiUrl = `${environment.apiUrl}/api/Organizations`;

  constructor(private http: HttpClient) {}

  /**
   * Get all organizations
   */
  getAll(): Observable<OrganizationDTO[]> {
    return this.http.get<OrganizationDTO[]>(this.apiUrl);
  }

  /**
   * Get organization by ID
   */
  getById(id: number): Observable<OrganizationDTO> {
    return this.http.get<OrganizationDTO>(`${this.apiUrl}/${id}`);
  }

  /**
   * US 2.2.5: Create new organization with initial representative
   */
  create(dto: CreateOrganizationDTO): Observable<OrganizationDTO> {
    return this.http.post<OrganizationDTO>(this.apiUrl, dto);
  }

  /**
   * Update organization
   */
  update(id: number, dto: Partial<OrganizationDTO>): Observable<OrganizationDTO> {
    return this.http.put<OrganizationDTO>(`${this.apiUrl}/${id}`, dto);
  }
}
