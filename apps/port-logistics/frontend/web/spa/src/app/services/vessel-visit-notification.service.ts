import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CrewMemberDTO {
  name: string;
  citizenId: string;
  nationality: string;
}

export interface VesselVisitNotificationDTO {
  id?: number;
  vesselImoNumber: string;
  estimatedTimeOfArrival?: string;
  estimatedTimeOfDeparture?: string;
  containerIds: string[];
  crew: CrewMemberDTO[];
  status: string;
  assignedDockCode?: string;
  rejectionReason?: string;
  submittedByRepresentativeId?: number;
  organizationIdentifier?: string;
  captainName?: string;
  crewCount?: number;
  safetyOfficers?: string[];
  preferredDockCode?: string;
  unloadingManifest?: any;
  loadingManifest?: any;
}

export interface VvnDecisionDTO {
  status: string; // 'Approved' | 'Rejected'
  assignedDockCode?: string;
  reason?: string;
}

export interface UpdateVvnStatusDTO {
  status: string;
}

export interface DecisionLogDTO {
  officerId: string;
  outcome: string;
  timestamp: string;
  assignedDockCode?: string;
  reason?: string;
}

export interface VvnFilterDTO {
  vesselImoNumber?: string;
  status?: string;
  representativeId?: number;
  etaFrom?: string;
  etaTo?: string;
  organizationId?: string;
}

export interface VesselVisitNotificationFilterDTO {
  vesselImoNumber?: string;
  status?: string;
  representativeId?: number;
  etaFrom?: string;
  etaTo?: string;
  organizationId?: string;
}

/**
 * Service for Vessel Visit Notifications
 * 
 * US 2.2.7: Port Officer reviews and approves/rejects notifications
 * US 2.2.8: Shipping Agent creates/submits notifications
 * US 2.2.9: Shipping Agent updates notifications in progress
 * US 2.2.10: View status and filter notifications
 */
@Injectable({
  providedIn: 'root'
})
export class VesselVisitNotificationService {
  private readonly apiUrl = `${environment.apiUrl}/api/vessel-visits/notifications`;

  constructor(private http: HttpClient) {}

  /**
   * US 2.2.8 & 2.2.10: Get all notifications with optional filters
   */
  getAll(filters?: VvnFilterDTO): Observable<VesselVisitNotificationDTO[]> {
    let params = new HttpParams();
    
    if (filters?.vesselImoNumber) {
      params = params.set('vesselImoNumber', filters.vesselImoNumber);
    }
    if (filters?.status) {
      params = params.set('status', filters.status);
    }
    if (filters?.representativeId) {
      params = params.set('representativeId', filters.representativeId.toString());
    }
    if (filters?.etaFrom) {
      params = params.set('etaFrom', filters.etaFrom);
    }
    if (filters?.etaTo) {
      params = params.set('etaTo', filters.etaTo);
    }
    if (filters?.organizationId) {
      params = params.set('organizationId', filters.organizationId);
    }

    return this.http.get<VesselVisitNotificationDTO[]>(this.apiUrl, { params });
  }

  /**
   * Get notification by ID
   */
  getById(id: number): Observable<VesselVisitNotificationDTO> {
    return this.http.get<VesselVisitNotificationDTO>(`${this.apiUrl}/${id}`);
  }

  /**
   * US 2.2.8: Create a new vessel visit notification
   */
  create(dto: VesselVisitNotificationDTO): Observable<VesselVisitNotificationDTO> {
    return this.http.post<VesselVisitNotificationDTO>(this.apiUrl, dto);
  }

  /**
   * US 2.2.9: Update notification (only when status is 'InProgress')
   */
  update(id: number, dto: VesselVisitNotificationDTO): Observable<VesselVisitNotificationDTO> {
    return this.http.put<VesselVisitNotificationDTO>(`${this.apiUrl}/${id}`, dto);
  }

  /**
   * US 2.2.8: Submit notification for approval (InProgress -> Submitted)
   */
  submit(id: number): Observable<VesselVisitNotificationDTO> {
    return this.http.patch<VesselVisitNotificationDTO>(
      `${this.apiUrl}/${id}/status`,
      { status: 'Submitted' }
    );
  }

  /**
   * US 2.2.7: Approve notification (Port Officer assigns dock)
   */
  approve(id: number, officerId: string, assignedDockCode: string): Observable<VesselVisitNotificationDTO> {
    const decision: VvnDecisionDTO = {
      status: 'Approved',
      assignedDockCode: assignedDockCode
    };
    
    return this.http.patch<VesselVisitNotificationDTO>(
      `${this.apiUrl}/${id}/decision?officerId=${officerId}`,
      decision
    );
  }

  /**
   * US 2.2.7: Reject notification with reason
   */
  reject(id: number, officerId: string, reason: string): Observable<VesselVisitNotificationDTO> {
    const decision: VvnDecisionDTO = {
      status: 'Rejected',
      reason: reason
    };
    
    return this.http.patch<VesselVisitNotificationDTO>(
      `${this.apiUrl}/${id}/decision?officerId=${officerId}`,
      decision
    );
  }

  /**
   * US 2.2.7: Get audit trail (decisions log)
   */
  getAudit(id: number): Observable<DecisionLogDTO[]> {
    return this.http.get<DecisionLogDTO[]>(`${this.apiUrl}/${id}/audit`);
  }

  /**
   * US 2.2.10: Get my own notifications
   */
  getMyNotifications(): Observable<VesselVisitNotificationDTO[]> {
    return this.http.get<VesselVisitNotificationDTO[]>(`${this.apiUrl}/my-notifications`);
  }

  /**
   * US 2.2.10: Get all notifications from my organization
   * Representative can see VVNs submitted by other representatives from same org
   */
  getOrganizationNotifications(): Observable<VesselVisitNotificationDTO[]> {
    return this.http.get<VesselVisitNotificationDTO[]>(`${this.apiUrl}/organization-notifications`);
  }
}
