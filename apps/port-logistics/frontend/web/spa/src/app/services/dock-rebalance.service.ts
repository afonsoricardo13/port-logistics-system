import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * US 4.3.3 - Dock Rebalance Service
 * 
 * Service for dock rebalancing operations
 * Allows Port Authority Officers to rebalance dock assignments before generating schedules
 */

export interface VesselDockAssignment {
  vvnId: number;
  vesselImoNumber: string;
  vesselName: string;
  estimatedArrival: string;
  estimatedDeparture: string;
  assignedDock: string;
  containerCount: number;
  estimatedDelay: number;
}

export interface DockRebalanceMetrics {
  totalExpectedDelay: number;
  averageDelayPerVessel: number;
  vesselsWithDelay: number;
  dockUtilization: { [dock: string]: number };
  dockCongestion: { [dock: string]: number };
  balanceScore: number;
}

export interface DockRebalanceProposal {
  date: string;
  currentAllocations: VesselDockAssignment[];
  proposedAllocations: VesselDockAssignment[];
  currentMetrics: DockRebalanceMetrics;
  proposedMetrics: DockRebalanceMetrics;
  recommendationReason: string;
}

export interface VesselDockReassignment {
  vvnId: number;
  originalDock: string;
  newDock: string;
}

export interface DockRebalanceConfirm {
  date: string;
  officerId: string;
  reassignments: VesselDockReassignment[];
}

export interface DockRebalanceResult {
  success: boolean;
  timestamp: string;
  officerId: string;
  reassignmentsApplied: number;
  messages: string[];
  finalMetrics: DockRebalanceMetrics;
}

@Injectable({
  providedIn: 'root'
})
export class DockRebalanceService {
  private baseUrl = `${environment.apiUrl}/api/DockRebalance`;

  constructor(private http: HttpClient) {}

  /**
   * Get rebalancing proposal for a specific date
   * Compares current vs proposed dock allocations with metrics
   */
  getProposal(date: string, officerId: string): Observable<DockRebalanceProposal> {
    const params = new HttpParams()
      .set('date', date)
      .set('officerId', officerId);

    return this.http.get<DockRebalanceProposal>(`${this.baseUrl}/proposal`, { params });
  }

  /**
   * Apply confirmed dock reassignments
   */
  applyRebalancing(confirmation: DockRebalanceConfirm): Observable<DockRebalanceResult> {
    return this.http.post<DockRebalanceResult>(`${this.baseUrl}/apply`, confirmation);
  }

  /**
   * Helper to calculate what changed between current and proposed allocations
   */
  calculateReassignments(
    currentAllocations: VesselDockAssignment[],
    proposedAllocations: VesselDockAssignment[]
  ): VesselDockReassignment[] {
    const reassignments: VesselDockReassignment[] = [];

    for (const proposed of proposedAllocations) {
      const current = currentAllocations.find(c => c.vvnId === proposed.vvnId);
      if (current && current.assignedDock !== proposed.assignedDock) {
        reassignments.push({
          vvnId: proposed.vvnId,
          originalDock: current.assignedDock,
          newDock: proposed.assignedDock
        });
      }
    }

    return reassignments;
  }

  /**
   * Calculate improvement percentage between metrics
   */
  calculateImprovement(current: DockRebalanceMetrics, proposed: DockRebalanceMetrics): number {
    if (current.totalExpectedDelay === 0) return 0;
    return ((current.totalExpectedDelay - proposed.totalExpectedDelay) / current.totalExpectedDelay) * 100;
  }
}
