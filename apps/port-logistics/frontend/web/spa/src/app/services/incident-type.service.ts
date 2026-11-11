import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface IncidentTypeDTO {
  code: string;
  name: string;
  description: string;
  severity: string;
  parentTypeCode?: string | null;
  parentTypeName?: string | null;
  childTypes: IncidentTypeDTO[];
}

export interface CreateIncidentTypeDTO {
  code: string;
  name: string;
  description: string;
  severity: string;
  parentTypeCode?: string | null;
}

export interface UpdateIncidentTypeDTO {
  name: string;
  description: string;
  severity: string;
  parentTypeCode?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class IncidentTypeService {
  private apiUrl = `${environment.oemApiUrl || 'https://localhost:7001'}/api/incidenttypes`;

  constructor(private http: HttpClient) {}

  getAll(rootOnly?: boolean, parentCode?: string): Observable<IncidentTypeDTO[]> {
    let params = new HttpParams();
    if (rootOnly !== undefined) {
      params = params.set('rootOnly', rootOnly.toString());
    }
    if (parentCode) {
      params = params.set('parentCode', parentCode);
    }
    return this.http.get<IncidentTypeDTO[]>(this.apiUrl, { params });
  }

  getByCode(code: string): Observable<IncidentTypeDTO> {
    return this.http.get<IncidentTypeDTO>(`${this.apiUrl}/${code}`);
  }

  create(dto: CreateIncidentTypeDTO): Observable<IncidentTypeDTO> {
    return this.http.post<IncidentTypeDTO>(this.apiUrl, dto);
  }

  update(code: string, dto: UpdateIncidentTypeDTO): Observable<IncidentTypeDTO> {
    return this.http.put<IncidentTypeDTO>(`${this.apiUrl}/${code}`, dto);
  }

  delete(code: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${code}`);
  }
}
