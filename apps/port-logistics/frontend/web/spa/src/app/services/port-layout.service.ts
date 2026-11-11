import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Position in 3D space
 */
export interface Position3D {
  x: number;
  y: number;
  z: number;
}

/**
 * 3D dimensions
 */
export interface Dimensions3D {
  length: number;
  width: number;
  height: number;
}

/**
 * Rotation in 3D space (Euler angles)
 */
export interface Rotation3D {
  x: number;
  y: number;
  z: number;
}

/**
 * Port element with 3D properties
 */
export interface PortElementDTO {
  id: string;
  type: string;
  name: string;
  position: Position3D;
  dimensions: Dimensions3D;
  rotation: Rotation3D;
  color: string;
  metadata: { [key: string]: string };
}

/**
 * Complete port layout
 */
export interface PortLayoutDTO {
  layoutId: string;
  layoutName: string;
  version?: string;
  description?: string;
  elements: PortElementDTO[];
  settings: { [key: string]: string };
}

/**
 * Service to fetch port layout data from backend API
 * US 3.3.2 - 3D Port Visualization with Real Data
 */
@Injectable({
  providedIn: 'root'
})
export class PortLayoutService {
  private apiUrl = `${environment.apiUrl}/api/port-layout`;

  constructor(private http: HttpClient) {}

  /**
   * Get port layout by ID
   * @param layoutId Layout identifier (e.g., "A", "B")
   * @returns Observable with port layout data
   */
  getLayoutById(layoutId: string): Observable<PortLayoutDTO> {
    return this.http.get<PortLayoutDTO>(`${this.apiUrl}?layoutId=${layoutId}`);
  }

  /**
   * Get available layout IDs
   * @returns Observable with array of available layout IDs
   */
  getAvailableLayouts(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/available`);
  }
}
