import { Injectable } from '@angular/core';

/**
 * Permissions Service
 * US 3.2.4: Frontend Authorization based on User Roles
 * 
 * Centralizes permission mappings between roles and features
 * Based on User Stories requirements from PART III specification
 */
@Injectable({
  providedIn: 'root'
})
export class PermissionsService {
  
  /**
   * Role to Permissions Mapping
   * Based on User Stories access requirements
   */
  private readonly rolePermissions: { [key: string]: string[] } = {
    'Administrator': [
      // Full access to everything
      'users',                          // US 3.2.5 - User Management
      'privacy-policy',                 // US 4.5.1 - Privacy Policy Management
      'vessel-types',                   // US 2.2.1 - Vessel Types
      'vessels',                        // US 2.2.2 - Vessels
      'docks',                          // US 2.2.3 - Docks
      'storage-areas',                  // US 2.2.4 - Storage Areas
      'organizations',                  // US 2.2.5 - Organizations
      'representatives',                // US 2.2.6 - Representatives
      'vvn-review',                     // US 2.2.7 - Review VVN
      'vessel-visit-notifications',     // US 2.2.8/9/10 - VVN CRUD
      'staff',                          // US 2.2.11 - Operating Staff
      'resources',                      // US 2.2.12 - Physical Resources
      'qualifications',                 // US 2.2.13 - Qualifications
      'scheduling',                     // US 3.4.2 - Daily Scheduling
      'viewer-3d',                      // US 3.3.1 - 3D Visualization
      'incident-types',                 // US 4.1.12 - Incident Types (OEM)
      'incidents',                      // US 4.1.13 - Incidents Management
      'task-categories',                // US 4.1.14 - Complementary Task Categories
      'complementary-tasks',            // US 4.1.15 - Complementary Tasks
      'operation-plans',                // US 4.1.2-6 - Operation Plans
      'vessel-visit-executions'         // US 4.1.7-11 - VVE Management
    ],
    'PortAuthorityOfficer': [
      'vessel-types',                   // US 2.2.1 - Create/Update Vessel Types
      'vessels',                        // US 2.2.2 - Register/Update Vessels
      'docks',                          // US 2.2.3 - Register/Update Docks
      'storage-areas',                  // US 2.2.4 - Register/Update Storage Areas
      'organizations',                  // US 2.2.5 - Register Organizations
      'representatives',                // US 2.2.6 - Manage Representatives
      'vvn-review',                     // US 2.2.7 - Review & Approve/Reject VVN
      'vessel-visit-notifications',     // US 2.2.10 - View VVN (read-only)
      'viewer-3d',                      // US 3.3.1 - 3D Visualization
      'incident-types'                  // US 4.1.12 - Incident Types (OEM)
    ],
    'LogisticsOperator': [
      'staff',                          // US 2.2.11 - Register/Manage Operating Staff
      'resources',                      // US 2.2.12 - Register/Manage Physical Resources
      'qualifications',                 // US 2.2.13 - Register/Manage Qualifications
      'scheduling',                     // US 3.4.2 - Daily Vessel Scheduling
      'docks',                          // View docks for scheduling (read-only)
      'vessel-visit-notifications',     // View VVN for scheduling (read-only)
      'viewer-3d',                      // US 3.3.1 - 3D Visualization
      'operation-plans',                // US 4.1.2-6 - Operation Plans Management
      'vessel-visit-executions',        // US 4.1.7-11 - VVE Management
      'incidents',                      // US 4.1.13 - Incidents Management
      'complementary-tasks'             // US 4.1.15 - Complementary Tasks
    ],
    'PortOperationsSupervisor': [
      'viewer-3d',                      // US 3.3.1 - 3D Visualization
      'task-categories'                 // US 4.1.14 - Complementary Task Categories
    ],
    'ShippingAgentRepresentative': [
      'vessel-visit-notifications',     // US 2.2.8 - Create VVN
                                        // US 2.2.9 - Update VVN (in progress)
                                        // US 2.2.10 - View VVN Status
      'vessels',                        // View vessels (read-only for VVN creation)
      'viewer-3d'                       // US 3.3.1 - 3D Visualization
    ]
  };

  /**
   * Get permissions for a specific role
   */
  getPermissionsForRole(role: string): string[] {
    return this.rolePermissions[role] || [];
  }

  /**
   * Check if a role has a specific permission
   */
  roleHasPermission(role: string, permission: string): boolean {
    const permissions = this.getPermissionsForRole(role);
    return permissions.includes(permission);
  }

  /**
   * Check if a role has any of the specified permissions
   */
  roleHasAnyPermission(role: string, requiredPermissions: string[]): boolean {
    const permissions = this.getPermissionsForRole(role);
    return requiredPermissions.some(p => permissions.includes(p));
  }

  /**
   * Get all available roles
   */
  getAllRoles(): string[] {
    return Object.keys(this.rolePermissions);
  }

  /**
   * Get all available permissions
   */
  getAllPermissions(): string[] {
    const allPermissions = new Set<string>();
    Object.values(this.rolePermissions).forEach(permissions => {
      permissions.forEach(p => allPermissions.add(p));
    });
    return Array.from(allPermissions).sort();
  }

  /**
   * Get human-readable role name
   */
  getRoleDisplayName(role: string): string {
    const displayNames: { [key: string]: string } = {
      'Administrator': 'Administrator',
      'PortAuthorityOfficer': 'Port Authority Officer',
      'LogisticsOperator': 'Logistics Operator',
      'PortOperationsSupervisor': 'Port Operations Supervisor',
      'ShippingAgentRepresentative': 'Shipping Agent Representative'
    };
    return displayNames[role] || role;
  }

  /**
   * Get feature description for permission
   */
  getPermissionDescription(permission: string): string {
    const descriptions: { [key: string]: string } = {
      'users': 'User Management (US 3.2.5)',
      'vessel-types': 'Vessel Types Management (US 2.2.1)',
      'vessels': 'Vessels Management (US 2.2.2)',
      'docks': 'Docks Management (US 2.2.3)',
      'storage-areas': 'Storage Areas Management (US 2.2.4)',
      'organizations': 'Organizations Management (US 2.2.5)',
      'representatives': 'Representatives Management (US 2.2.6)',
      'vvn-review': 'Review & Approve VVN (US 2.2.7)',
      'vessel-visit-notifications': 'Vessel Visit Notifications (US 2.2.8/9/10)',
      'staff': 'Operating Staff Management (US 2.2.11)',
      'resources': 'Physical Resources Management (US 2.2.12)',
      'qualifications': 'Qualifications Management (US 2.2.13)',
      'scheduling': 'Daily Scheduling (US 3.4.2)',
      'viewer-3d': '3D Visualization (US 3.3.1)',
      'privacy-policy': 'Privacy Policy Management (US 4.5.1)',
      'incident-types': 'Incident Types Management (US 4.1.12)',
      'incidents': 'Incidents Management (US 4.1.13)',
      'task-categories': 'Complementary Task Categories (US 4.1.14)',
      'complementary-tasks': 'Complementary Tasks Management (US 4.1.15)',
      'operation-plans': 'Operation Plans Management (US 4.1.2-6)',
      'vessel-visit-executions': 'Vessel Visit Executions (US 4.1.7-11)'
    };
    return descriptions[permission] || permission;
  }
}
