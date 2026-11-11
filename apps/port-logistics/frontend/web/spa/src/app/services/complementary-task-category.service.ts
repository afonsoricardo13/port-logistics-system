import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ComplementaryTaskCategoryDTO {
  code: string;
  name: string;
  description: string;
  defaultDuration?: number | null;
  expectedImpact?: string | null;
}

export interface CreateComplementaryTaskCategoryDTO {
  code: string;
  name: string;
  description: string;
  defaultDuration?: number | null;
  expectedImpact?: string | null;
}

export interface UpdateComplementaryTaskCategoryDTO {
  name: string;
  description: string;
  defaultDuration?: number | null;
  expectedImpact?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class ComplementaryTaskCategoryService {
  private apiUrl = `${environment.oemApiUrl || 'https://localhost:7001'}/api/complementarytaskcategories`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ComplementaryTaskCategoryDTO[]> {
    return this.http.get<ComplementaryTaskCategoryDTO[]>(this.apiUrl);
  }

  getByCode(code: string): Observable<ComplementaryTaskCategoryDTO> {
    return this.http.get<ComplementaryTaskCategoryDTO>(`${this.apiUrl}/${code}`);
  }

  create(dto: CreateComplementaryTaskCategoryDTO): Observable<ComplementaryTaskCategoryDTO> {
    return this.http.post<ComplementaryTaskCategoryDTO>(this.apiUrl, dto);
  }

  update(code: string, dto: UpdateComplementaryTaskCategoryDTO): Observable<ComplementaryTaskCategoryDTO> {
    return this.http.put<ComplementaryTaskCategoryDTO>(`${this.apiUrl}/${code}`, dto);
  }

  delete(code: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${code}`);
  }
}
