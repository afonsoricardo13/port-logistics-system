import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface VesselDTO {
  imoNumber: string; // 7 digits with check digit
  name: string;
  vesselType: string;
  operator: string; // operator/owner
}

export interface VesselSearchParams {
  imoNumber?: string;
  name?: string;
  operatorName?: string;
}

/**
 * Service for Vessels Management
 * 
 * US 2.2.2: Port Authority Officer registers and updates vessel records
 */
@Injectable({
  providedIn: 'root'
})
export class VesselManagementService {
  private readonly apiUrl = `${environment.apiUrl}/api/Vessels`;

  constructor(private http: HttpClient) {}

  /**
   * Search and filter vessels
   */
  search(params?: VesselSearchParams): Observable<VesselDTO[]> {
    let httpParams = new HttpParams();
    
    if (params?.imoNumber) {
      httpParams = httpParams.set('imoNumber', params.imoNumber);
    }
    if (params?.name) {
      httpParams = httpParams.set('name', params.name);
    }
    if (params?.operatorName) {
      httpParams = httpParams.set('operatorName', params.operatorName);
    }

    return this.http.get<VesselDTO[]>(this.apiUrl, { params: httpParams });
  }

  /**
   * Get all vessels
   */
  getAll(): Observable<VesselDTO[]> {
    return this.http.get<VesselDTO[]>(this.apiUrl);
  }

  /**
   * Get vessel by IMO number
   */
  getByImo(imoNumber: string): Observable<VesselDTO> {
    return this.http.get<VesselDTO>(`${this.apiUrl}/${imoNumber}`);
  }

  /**
   * Create a new vessel
   */
  create(dto: VesselDTO): Observable<VesselDTO> {
    return this.http.post<VesselDTO>(this.apiUrl, dto);
  }

  /**
   * Update an existing vessel
   */
  update(imoNumber: string, dto: VesselDTO): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${imoNumber}`, dto);
  }

  /**
   * Validate IMO number format (7 digits with check digit)
   * IMO format: "IMO" prefix is optional, followed by 7 digits.
   * Check digit calculation: multiply first 6 digits by 7,6,5,4,3,2 respectively, sum them, and mod 10.
   */
  validateIMO(imoNumber: string): boolean {
    if (!imoNumber) {
      return false;
    }

    // Remove "IMO" prefix if present and trim
    let digits = imoNumber.trim().toUpperCase();
    if (digits.startsWith('IMO')) {
      digits = digits.substring(3).trim();
    }

    // Must be exactly 7 digits
    if (digits.length !== 7 || !/^\d{7}$/.test(digits)) {
      return false;
    }

    // Calculate check digit
    const multipliers = [7, 6, 5, 4, 3, 2];
    let sum = 0;
    
    for (let i = 0; i < 6; i++) {
      sum += parseInt(digits[i]) * multipliers[i];
    }

    const checkDigit = sum % 10;
    const lastDigit = parseInt(digits[6]);

    return checkDigit === lastDigit;
  }

  /**
   * Format IMO number with "IMO" prefix
   */
  formatIMO(imoNumber: string): string {
    if (!imoNumber) return '';
    
    let digits = imoNumber.trim().toUpperCase();
    if (digits.startsWith('IMO')) {
      digits = digits.substring(3).trim();
    }

    return `IMO${digits}`;
  }
}
