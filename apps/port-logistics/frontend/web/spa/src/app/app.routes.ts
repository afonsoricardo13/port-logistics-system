import { Routes } from '@angular/router';
import { Layout } from './components/layout/layout';
import { Dashboard } from './pages/dashboard/dashboard';
import { LoginComponent } from './pages/login/login';
import { CallbackComponent } from './pages/callback/callback';
import { AccessDeniedComponent } from './pages/access-denied/access-denied';
import { ActivateComponent } from './pages/activate/activate';
import { UserListComponent } from './pages/user-management/user-list';
import { UserFormComponent } from './pages/user-management/user-form';
import { Viewer3dComponent } from './pages/viewer-3d/viewer-3d';
import { NoAccess } from './components/no-access/no-access';
import { DailySchedulingComponent } from './pages/daily-scheduling/daily-scheduling';
import { VvnListComponent } from './pages/vessel-visit-notifications/list/vvn-list.component';
import { VvnFormComponent } from './pages/vessel-visit-notifications/form/vvn-form.component';
import { VvnReviewComponent } from './pages/vessel-visit-notifications/review/vvn-review.component';
import { VvnDetailsComponent } from './pages/vessel-visit-notifications/details/vvn-details.component';
import { StorageAreasListComponent } from './pages/storage-areas/storage-areas-list.component';
import { StorageAreasFormComponent } from './pages/storage-areas/storage-areas-form.component';
import { PhysicalResourcesListComponent } from './pages/physical-resources/physical-resources-list.component';
import { PhysicalResourcesFormComponent } from './pages/physical-resources/physical-resources-form.component';
import { DocksListComponent } from './pages/docks/docks-list.component';
import { DocksFormComponent } from './pages/docks/docks-form.component';
import { VesselsListComponent } from './pages/vessels/vessels-list.component';
import { VesselsFormComponent } from './pages/vessels/vessels-form.component';
import { VesselTypesListComponent } from './pages/vessel-types/vessel-types-list.component';
import { VesselTypesFormComponent } from './pages/vessel-types/vessel-types-form.component';
import { OrganizationsListComponent } from './pages/organizations/organizations-list.component';
import { OrganizationsFormComponent } from './pages/organizations/organizations-form.component';
import { RepresentativesListComponent } from './pages/representatives/representatives-list.component';
import { RepresentativesFormComponent } from './pages/representatives/representatives-form.component';
import { StaffListComponent } from './pages/operating-staff/list/staff-list.component';
import { StaffFormComponent } from './pages/operating-staff/form/staff-form.component';
import { StaffDetailComponent } from './pages/operating-staff/detail/staff-detail.component';
import { QualificationsListComponent } from './components/qualifications/qualifications-list.component';
import { QualificationFormComponent } from './components/qualifications/qualification-form.component';
import { IncidentTypesListComponent } from './pages/incident-types/incident-types-list.component';
import { IncidentTypesFormComponent } from './pages/incident-types/incident-types-form.component';
import { IncidentsListComponent } from './pages/incidents/incidents-list.component';
import { IncidentsFormComponent } from './pages/incidents/incidents-form.component';
import { ComplementaryTaskCategoriesListComponent } from './pages/complementary-task-categories/complementary-task-categories-list.component';
import { ComplementaryTaskCategoriesFormComponent } from './pages/complementary-task-categories/complementary-task-categories-form.component';
import { ComplementaryTasksListComponent } from './pages/complementary-tasks/complementary-tasks-list.component';
import { ComplementaryTasksFormComponent } from './pages/complementary-tasks/complementary-tasks-form.component';
import { OperationsHomeComponent } from './pages/operations-home/operations-home.component';
import { OperationPlansComponent } from './pages/operation-plans/operation-plans.component';
import { OperationPlanUpdateComponent } from './components/operation-plan-update/operation-plan-update.component';
import { VveListComponent } from './pages/vessel-visit-executions/vve-list.component';
import { VveCreateComponent } from './pages/vessel-visit-executions/vve-create.component';
import { VveBerthingComponent } from './pages/vessel-visit-executions/vve-berthing/vve-berthing.component';
import { VveSearchComponent } from './pages/vessel-visit-executions/vve-search.component';
import { VveExecutionTrackingComponent } from './pages/vve-execution-tracking/vve-execution-tracking.component';
import { PrivacyPolicy } from './pages/privacy-policy/privacy-policy';
import { PrivacyPolicyAdmin } from './pages/privacy-policy-admin/privacy-policy-admin';
import { DataRights } from './pages/data-rights/data-rights';
import { DataRightsAdmin } from './pages/data-rights-admin/data-rights-admin';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';
import { permissionGuard } from './guards/permission.guard';
import { ResourceUsageComponent } from './pages/resource-usage/resource-usage.component';

export const routes: Routes = [
  // Public routes (no authentication required)
  { path: 'login', component: LoginComponent },
  { path: 'callback', component: CallbackComponent },
  { path: 'access-denied', component: AccessDeniedComponent }, // US 3.2.2
  { path: 'no-access', component: NoAccess }, // US 3.2.4 - Permission denied
  { path: 'activate', component: ActivateComponent }, // US 3.2.6 - Account activation
  { path: 'privacy-policy', component: PrivacyPolicy }, // US 4.5.1 - Public Privacy Policy

  // Protected routes (authentication required - US 3.2.1)
  {
    path: '',
    component: Layout, // Layout wrapper com navbar sempre visível
    canActivate: [authGuard], // Protect all child routes with authentication
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: Dashboard },

      // US 3.2.5 - User Management (Administrator only - US 3.2.4)
      {
        path: 'user-management',
        component: UserListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'users' }
      },
      {
        path: 'user-management/create',
        component: UserFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'users' }
      },
      {
        path: 'user-management/edit/:email',
        component: UserFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'users' }
      },

      // US 3.3.1 - 3D Visualization (protected route)
      {
        path: '3d-viewer',
        component: Viewer3dComponent
      },

      // US 2.2.10 - Vessel Visit Notifications List (US 2.2.7/8/9/10)
      {
        path: 'vessel-visits',
        component: VvnListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vessel-visit-notifications' }
      },
      // US 2.2.8 - Create Vessel Visit Notification
      {
        path: 'vessel-visits/create',
        component: VvnFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vessel-visit-notifications' }
      },
      // US 2.2.9 - Edit Vessel Visit Notification
      {
        path: 'vessel-visits/edit/:id',
        component: VvnFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vessel-visit-notifications' }
      },
      // US 2.2.10 - View Vessel Visit Notification Details
      {
        path: 'vessel-visits/:id',
        component: VvnDetailsComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vessel-visit-notifications' }
      },
      // US 2.2.7 - Review & Approve/Reject (Port Officer)
      {
        path: 'vessel-visits/:id/review',
        component: VvnReviewComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vvn-review' }
      },

      // US 2.2.4 - Storage Areas Management (Port Authority Officer only)
      {
        path: 'storage-areas',
        component: StorageAreasListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'storage-areas' }
      },
      {
        path: 'storage-areas/new',
        component: StorageAreasFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'storage-areas' }
      },
      {
        path: 'storage-areas/:identifier/edit',
        component: StorageAreasFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'storage-areas' }
      },

      // US 2.2.12 - Physical Resources Management (Logistics Operator)
      {
        path: 'physical-resources',
        component: PhysicalResourcesListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'resources' }
      },
      {
        path: 'physical-resources/new',
        component: PhysicalResourcesFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'resources' }
      },
      {
        path: 'physical-resources/:code/edit',
        component: PhysicalResourcesFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'resources' }
      },

      // US 2.2.3 - Docks Management (Port Authority Officer)
      {
        path: 'docks',
        component: DocksListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'docks' }
      },
      {
        path: 'docks/new',
        component: DocksFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'docks' }
      },
      {
        path: 'docks/:code/edit',
        component: DocksFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'docks' }
      },

      // US 2.2.2 - Vessels Management (Port Authority Officer, Shipping Agent Representative)
      {
        path: 'vessels',
        component: VesselsListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vessels' }
      },
      {
        path: 'vessels/new',
        component: VesselsFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vessels' }
      },
      {
        path: 'vessels/:imo/edit',
        component: VesselsFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vessels' }
      },

      // US 2.2.1 - Vessel Types Management (Administrator, Port Authority Officer)
      {
        path: 'vessel-types',
        component: VesselTypesListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vessel-types' }
      },
      {
        path: 'vessel-types/new',
        component: VesselTypesFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vessel-types' }
      },
      {
        path: 'vessel-types/:name/edit',
        component: VesselTypesFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'vessel-types' }
      },

      // US 2.2.5 - Organizations Management (Port Authority Officer)
      {
        path: 'organizations',
        component: OrganizationsListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'organizations' }
      },
      {
        path: 'organizations/create',
        component: OrganizationsFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'organizations' }
      },
      {
        path: 'organizations/edit/:id',
        component: OrganizationsFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'organizations' }
      },

      // US 2.2.6 - Representatives Management (Port Authority Officer)
      {
        path: 'representatives',
        component: RepresentativesListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'representatives' }
      },
      {
        path: 'representatives/create',
        component: RepresentativesFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'representatives' }
      },
      {
        path: 'representatives/edit/:id',
        component: RepresentativesFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'representatives' }
      },

      // US 2.2.11 & 3.1.5 - Operating Staff Management (Logistics Operator)
      {
        path: 'operating-staff',
        component: StaffListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'staff' }
      },
      {
        path: 'operating-staff/create',
        component: StaffFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'staff' }
      },
      {
        path: 'operating-staff/edit/:number',
        component: StaffFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'staff' }
      },
      {
        path: 'operating-staff/:number',
        component: StaffDetailComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'staff' }
      },

      // US 2.2.13 - Qualifications Management (Logistics Operator)
      {
        path: 'qualifications',
        component: QualificationsListComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'qualifications' }
      },
      {
        path: 'qualifications/new',
        component: QualificationFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'qualifications' }
      },
      {
        path: 'qualifications/:name/edit',
        component: QualificationFormComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'qualifications' }
      },

      // US 3.4.2 - Daily Vessel Scheduling (Logistics Operator)
      {
        path: 'scheduling/daily',
        component: DailySchedulingComponent,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'scheduling' }
      },

      // US 4.5.1 - Privacy Policy Management (Administrator)
      {
        path: 'admin/privacy-policy',
        component: PrivacyPolicyAdmin,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'privacy-policy' }
      },

      // US 4.5.3 - Data Rights (All authenticated users)
      { 
        path: 'data-rights', 
        component: DataRights,
        canActivate: [authGuard]
      },

      // US 4.5.3 - Data Rights Admin (Administrator only)
      { 
        path: 'admin/data-rights', 
        component: DataRightsAdmin,
        canActivate: [permissionGuard],
        data: { requiredPermission: 'privacy-policy' }
      },

      // US 4.1.x - Operations & Execution Management
      {
        path: 'operations',
        component: OperationsHomeComponent,
        canActivate: [authGuard],
        children: [
          // US 4.1.12 - Incident Types (Port Authority Officer)
          {
            path: 'incident-types',
            component: IncidentTypesListComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'incident-types' }
          },
          {
            path: 'incident-types/new',
            component: IncidentTypesFormComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'incident-types' }
          },
          {
            path: 'incident-types/:code/edit',
            component: IncidentTypesFormComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'incident-types' }
          },
          // US 4.1.13 - Incidents Management (Logistics Operator)
          {
            path: 'incidents',
            component: IncidentsListComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'incidents' }
          },
          {
            path: 'incidents/create',
            component: IncidentsFormComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'incidents' }
          },
          {
            path: 'incidents/edit/:id',
            component: IncidentsFormComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'incidents' }
          },
          // US 4.1.14 - Complementary Task Categories (Port Operations Supervisor)
          {
            path: 'task-categories',
            component: ComplementaryTaskCategoriesListComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'task-categories' }
          },
          {
            path: 'task-categories/new',
            component: ComplementaryTaskCategoriesFormComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'task-categories' }
          },
          {
            path: 'task-categories/:code/edit',
            component: ComplementaryTaskCategoriesFormComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'task-categories' }
          },
          // US 4.1.15 - Complementary Tasks Management (Logistics Operator)
          {
            path: 'complementary-tasks',
            component: ComplementaryTasksListComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'complementary-tasks' }
          },
          {
            path: 'complementary-tasks/create',
            component: ComplementaryTasksFormComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'complementary-tasks' }
          },
          {
            path: 'complementary-tasks/edit/:id',
            component: ComplementaryTasksFormComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'complementary-tasks' }
          },
          // US 4.1.2-6 - Operation Plans (Logistics Operator)
          {
            path: 'operation-plans/update/:id',
            component: OperationPlanUpdateComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'operation-plans' }
          },
          {
            path: 'operation-plans',
            component: OperationPlansComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'operation-plans' }
          },
          {
            path: 'resource-usage',
            component: ResourceUsageComponent
          },
          // US 4.1.7-11 - Vessel Visit Executions (Logistics Operator)
          {
            path: 'executions',
            component: VveListComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'vessel-visit-executions' }
          },
          {
            path: 'executions/search',
            component: VveSearchComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'vessel-visit-executions' }
          },
          {
            path: 'executions/create',
            component: VveCreateComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'vessel-visit-executions' }
          },
          {
            path: 'executions/:id/berthing',
            component: VveBerthingComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'vessel-visit-executions' }
          },
          {
            path: 'executions/:vveId/track',
            component: VveExecutionTrackingComponent,
            canActivate: [permissionGuard],
            data: { requiredPermission: 'vessel-visit-executions' }
          },
          { path: 'tasks', redirectTo: 'complementary-tasks' }
        ]
      },
    ]
  }
];

