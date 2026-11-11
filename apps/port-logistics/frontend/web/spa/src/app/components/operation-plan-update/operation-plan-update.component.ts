import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { 
  OperationPlanningService, 
  UpdateOperationPlanRequest,
  UpdateOperationPlanResponse,
  OperationPlanDetailed,
  ResourceAssignment 
} from '../../services/operation-planning.service';

/**
 * US 4.1.4 - Component for manually updating operation plans
 */
@Component({
  selector: 'app-operation-plan-update',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './operation-plan-update.component.html',
  styleUrls: ['./operation-plan-update.component.scss']
})
export class OperationPlanUpdateComponent implements OnInit {
  planId: string = '';
  operationPlan?: OperationPlanDetailed;
  loading = false;
  error = '';
  successMessage = '';

  // Form fields
  reasonForChange = '';
  newCraneId = '';
  newStaffId = '';
  newDock = '';
  newStartTime = '';
  newEndTime = '';

  // Response data
  warnings: string[] = [];
  resourceConflicts: any[] = [];
  vvnInconsistencies: any[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private operationPlanService: OperationPlanningService
  ) {}

  ngOnInit() {
    // Get plan ID from route parameter
    this.planId = this.route.snapshot.paramMap.get('id') || '';
    if (this.planId) {
      this.loadOperationPlan();
    }
  }

  loadOperationPlan() {
    this.loading = true;
    this.error = '';
    
    this.operationPlanService.getOperationPlanById(this.planId).subscribe({
      next: (plan) => {
        this.operationPlan = plan;
        this.loading = false;
        
        // Pre-fill form with current values
        if (plan.startTime) {
          this.newStartTime = new Date(plan.startTime).toISOString().slice(0, 16);
        }
        if (plan.endTime) {
          this.newEndTime = new Date(plan.endTime).toISOString().slice(0, 16);
        }
        if (plan.assignedDock) {
          this.newDock = plan.assignedDock;
        }
      },
      error: (err) => {
        this.error = `Failed to load operation plan: ${err.message}`;
        this.loading = false;
      }
    });
  }

  updateOperationPlan() {
    if (!this.reasonForChange.trim()) {
      this.error = 'Reason for change is required';
      return;
    }

    this.loading = true;
    this.error = '';
    this.successMessage = '';
    this.warnings = [];
    this.resourceConflicts = [];
    this.vvnInconsistencies = [];

    const request: UpdateOperationPlanRequest = {
      reasonForChange: this.reasonForChange
    };

    // Add crane assignments if provided (name will be fetched from database)
    if (this.newCraneId.trim()) {
      request.craneAssignments = [{
        type: 'crane',
        id: this.newCraneId.trim()
      }];
    }

    // Add staff assignments if provided (name will be fetched from database)
    if (this.newStaffId.trim()) {
      request.staffAssignments = [{
        type: 'staff',
        id: this.newStaffId.trim()
      }];
    }

    // Add dock if changed
    if (this.newDock && this.newDock !== this.operationPlan?.assignedDock) {
      request.assignedDock = this.newDock;
    }

    // Add time changes if provided
    if (this.newStartTime) {
      request.startTime = new Date(this.newStartTime).toISOString();
    }
    if (this.newEndTime) {
      request.endTime = new Date(this.newEndTime).toISOString();
    }

    // Call the update service
    this.operationPlanService.updateOperationPlan(this.planId, request).subscribe({
      next: (response: UpdateOperationPlanResponse) => {
        this.loading = false;
        
        if (response.success) {
          this.successMessage = response.message;
          this.operationPlan = response.updatedPlan;
          
          // Show warnings and conflicts
          this.warnings = response.warnings;
          this.resourceConflicts = response.resourceConflicts;
          this.vvnInconsistencies = response.vvnInconsistencies;
          
          // Clear form
          this.reasonForChange = '';
          this.newCraneId = '';
          this.newStaffId = '';
          
          // Reload to show updated data
          setTimeout(() => this.loadOperationPlan(), 2000);
        } else {
          this.error = response.message;
        }
      },
      error: (err) => {
        this.error = `Update failed: ${err.error?.message || err.message}`;
        this.loading = false;
      }
    });
  }

  clearChangeHistory() {
    if (!confirm('Are you sure you want to clear the change history? This action cannot be undone.')) {
      return;
    }

    this.loading = true;
    this.error = '';
    this.successMessage = '';

    this.operationPlanService.clearChangeLogs(this.planId).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.success) {
          this.successMessage = 'Change history cleared successfully';
          this.loadOperationPlan(); // Reload to reflect changes
        } else {
          this.error = response.message;
        }
      },
      error: (err) => {
        this.error = `Failed to clear change history: ${err.error?.message || err.message}`;
        this.loading = false;
      }
    });
  }

  getResourceByType(type: string): ResourceAssignment | undefined {
    return this.operationPlan?.resources?.find(r => r.type === type);
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString();
  }
}
