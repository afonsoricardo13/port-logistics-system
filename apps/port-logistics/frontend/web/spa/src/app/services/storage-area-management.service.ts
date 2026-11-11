import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ServedDockDTO {
  dockCode: string;
  distanceMeters: number;
}

export interface StorageAreaDTO {
  identifier: string;
  type: string; // 'Yard' | 'Warehouse'
  location: string;
  maxCapacityTeu: number;
  currentOccupancyTeu: number;
  servedDocks: ServedDockDTO[];
}

/**
 * Service for Storage Areas CRUD operations
 * 
 * US 2.2.4: Port Authority Officer registers and updates storage areas
 */
@Injectable({
  providedIn: 'root'
})
export class StorageAreaManagementService {
  private readonly apiUrl = `${environment.apiUrl}/api/storage-areas`;

  constructor(private http: HttpClient) {}

  /**
   * Get all storage areas
   */
  getAll(): Observable<StorageAreaDTO[]> {
    return this.http.get<StorageAreaDTO[]>(this.apiUrl);
  }

  /**
   * Get storage area by identifier
   */
  getByIdentifier(identifier: string): Observable<StorageAreaDTO> {
    return this.http.get<StorageAreaDTO>(`${this.apiUrl}/${identifier}`);
  }

  /**
   * Create a new storage area
   */
  create(dto: StorageAreaDTO): Observable<StorageAreaDTO> {
    return this.http.post<StorageAreaDTO>(this.apiUrl, dto);
  }

  /**
   * Update an existing storage area
   */
  update(identifier: string, dto: StorageAreaDTO): Observable<StorageAreaDTO> {
    return this.http.put<StorageAreaDTO>(`${this.apiUrl}/${identifier}`, dto);
  }

  /**
   * Calculate occupancy percentage
   */
  getOccupancyPercentage(area: StorageAreaDTO): number {
    if (area.maxCapacityTeu === 0) return 0;
    return Math.round((area.currentOccupancyTeu / area.maxCapacityTeu) * 100);
  }

  /**
   * Get available capacity
   */
  getAvailableCapacity(area: StorageAreaDTO): number {
    return area.maxCapacityTeu - area.currentOccupancyTeu;
  }

  /**
   * Check if area serves all docks (no specific docks assigned)
   */
  servesAllDocks(area: StorageAreaDTO): boolean {
    return !area.servedDocks || area.servedDocks.length === 0;
  }
}
