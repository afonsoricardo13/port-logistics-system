import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OperationPlanningService, ResourceUsageResult } from '../../services/operation-planning.service';

@Component({
  selector: 'app-resource-usage',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './resource-usage.component.html',
  styleUrls: ['./resource-usage.component.scss']
})
export class ResourceUsageComponent {
  resourceType = 'crane';
  resourceId = '';
  startDate = '';
  endDate = '';
  loading = false;
  error: string | null = null;
  result: ResourceUsageResult | null = null;

  constructor(private planning: OperationPlanningService) {}

  query() {
    this.error = null;
    this.result = null;

    if (!this.resourceId || !this.startDate || !this.endDate) {
      this.error = 'Please fill all fields';
      return;
    }

    this.loading = true;
    this.planning.getResourceUsage(this.resourceType, this.resourceId, this.startDate, this.endDate)
      .subscribe({
        next: (res) => {
          this.result = res as any;
          this.loading = false;
        },
        error: (err) => {
          this.error = err?.error?.message || err?.message || 'Error fetching resource usage';
          this.loading = false;
        }
      });
  }

  // Helper used from template to compute total usage across details (avoids inline arrow functions in template)
  computeDetailsTotal(): number {
    if (!this.result || !Array.isArray(this.result.details)) return 0;
    return this.result.details.reduce((acc, d) => acc + (d?.usage || 0), 0);
  }
}
