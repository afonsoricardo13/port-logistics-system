import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  ExecutedOperationService,
  ExecutedOperationDTO,
  ResourceUsageDTO,
  InitializeFromPlanRequest,
  UpdateExecutedOperationDTO
} from '../../services/oem/executed-operation.service';
import { VesselVisitExecutionService, VesselVisitExecutionDTO } from '../../services/oem/vessel-visit-execution.service';
import { OperationPlanningService, OperationPlanDetailed } from '../../services/operation-planning.service';

@Component({
  selector: 'app-vve-execution-tracking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './vve-execution-tracking.component.html',
  styleUrl: './vve-execution-tracking.component.scss'
})
export class VveExecutionTrackingComponent implements OnInit {
  vveId = signal<string>('');
  vve = signal<VesselVisitExecutionDTO | null>(null);
  operations = signal<ExecutedOperationDTO[]>([]);
  operationPlan = signal<OperationPlanDetailed | null>(null);
  loading = signal<boolean>(false);
  error = signal<string>('');

  // Edit form
  editingOperation = signal<ExecutedOperationDTO | null>(null);
  editForm = signal<{
    actualStartTime: string;
    actualEndTime: string;
    actualResources: ResourceUsageDTO[];
    notes: string;
  }>({
    actualStartTime: '',
    actualEndTime: '',
    actualResources: [],
    notes: ''
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private executedOperationService: ExecutedOperationService,
    private vveService: VesselVisitExecutionService,
    private operationPlanningService: OperationPlanningService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('vveId');
    if (id) {
      this.vveId.set(id);
      this.loadVveDetails();
      this.loadOperations();
    }
  }

  loadVveDetails(): void {
    this.loading.set(true);
    this.vveService.getById(this.vveId()).subscribe({
      next: (vve) => {
        this.vve.set(vve);
        // Load operation plan using VVN filter (backend supports query)
        this.operationPlanningService.getOperationPlansByVvnId(vve.vvnId).subscribe({
          next: (plans) => {
            this.operationPlan.set(plans.length > 0 ? plans[0] : null);
            this.loading.set(false);
          },
          error: (err) => {
            if (err.status === 404) {
              this.operationPlan.set(null);
              this.showPopup('Não existe Operation Plan para este VVN.');
            } else {
              this.error.set('Erro ao carregar Operation Plan');
            }
            this.loading.set(false);
          }
        });
      },
      error: () => {
        this.error.set('Failed to load VVE details');
        this.loading.set(false);
      }
    });
  }

  loadOperations(): void {
    this.loading.set(true);
    this.executedOperationService.getByVveId(this.vveId()).subscribe({
      next: (ops) => {
        this.operations.set(ops);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load operations');
        this.loading.set(false);
      }
    });
  }

  initializeFromPlan(): void {
    const plan = this.operationPlan();
    if (!plan) {
      this.showPopup('Não existe Operation Plan para este VVN.');
      return;
    }

    const request: InitializeFromPlanRequest = {
      vveId: this.vveId(),
      operationPlanId: plan.id
    };

    this.loading.set(true);
    this.executedOperationService.initializeFromPlan(request).subscribe({
      next: (response) => {
        if (response.success) {
          this.loadOperations();
        } else {
          this.error.set(response.message);
          this.loading.set(false);
        }
      },
      error: () => {
        this.error.set('Failed to initialize operations from plan');
        this.loading.set(false);
      }
    });
  }

  // Preview operations from the plan without persisting
  loadOperationsFromPlan(): void {
    const plan = this.operationPlan();
    if (!plan || !plan.operations) {
      this.showPopup('Não existe Operation Plan para este VVN.');
      return;
    }

    let ops: any[] = [];
    if (Array.isArray(plan.operations)) {
      ops = plan.operations;
    } else if (typeof plan.operations === 'string') {
      try {
        ops = JSON.parse(plan.operations);
      } catch {
        this.showPopup('Formato inválido das operações no plano.');
        return;
      }
    }

    const previewOps = ops.map((op, idx) => ({
      id: 'preview-' + idx,
      vveId: this.vveId(),
      operationPlanId: plan.id,
      plannedOperationIndex: idx,
      type: op.type || op.Type || 'Unknown',
      status: 'Started',
      plannedStartTime: op.startTime || op.start || '',
      plannedEndTime: op.endTime || op.end || '',
      actualStartTime: '',
      actualEndTime: '',
      actualResources: [],
      delayMinutes: 0,
      updatedBy: '',
      updatedAt: '',
      createdAt: '',
      notes: '[Preview from Plan]'
    }));

    this.operations.set(previewOps as ExecutedOperationDTO[]);
  }

  quickStart(operation: ExecutedOperationDTO): void {
    const updated: UpdateExecutedOperationDTO = {
      actualStartTime: new Date().toISOString(),
      status: 'Started'
    };

    this.executedOperationService.update(operation.id, updated).subscribe({
      next: () => this.loadOperations(),
      error: () => this.error.set('Failed to start operation')
    });
  }

  quickComplete(operation: ExecutedOperationDTO): void {
    const updated: UpdateExecutedOperationDTO = {
      actualEndTime: new Date().toISOString(),
      status: 'Completed'
    };

    this.executedOperationService.update(operation.id, updated).subscribe({
      next: () => this.loadOperations(),
      error: () => this.error.set('Failed to complete operation')
    });
  }

  quickDelayed(operation: ExecutedOperationDTO): void {
    const updated: UpdateExecutedOperationDTO = {
      status: 'Delayed'
    };

    this.executedOperationService.update(operation.id, updated).subscribe({
      next: () => this.loadOperations(),
      error: () => this.error.set('Failed to mark as delayed')
    });
  }

  editOperation(operation: ExecutedOperationDTO): void {
    this.editingOperation.set(operation);
    this.editForm.set({
      actualStartTime: operation.actualStartTime || '',
      actualEndTime: operation.actualEndTime || '',
      actualResources: operation.actualResources || [],
      notes: operation.notes || ''
    });
  }

  saveEdit(): void {
    const op = this.editingOperation();
    if (!op) return;

    const form = this.editForm();
    const updated: UpdateExecutedOperationDTO = {
      actualStartTime: form.actualStartTime || undefined,
      actualEndTime: form.actualEndTime || undefined,
      actualResources: form.actualResources,
      notes: form.notes
    };

    this.executedOperationService.update(op.id, updated).subscribe({
      next: () => {
        this.editingOperation.set(null);
        this.loadOperations();
      },
      error: () => this.error.set('Failed to update operation')
    });
  }

  cancelEdit(): void {
    this.editingOperation.set(null);
  }

  deleteOperation(operation: ExecutedOperationDTO): void {
    if (!confirm('Are you sure you want to delete this operation?')) return;

    this.executedOperationService.delete(operation.id).subscribe({
      next: () => this.loadOperations(),
      error: () => this.error.set('Failed to delete operation')
    });
  }

  addResource(): void {
    const form = this.editForm();
    form.actualResources.push({
      resourceType: '',
      resourceId: '',
      quantity: 0
    });
    this.editForm.set({ ...form });
  }

  removeResource(index: number): void {
    const form = this.editForm();
    form.actualResources.splice(index, 1);
    this.editForm.set({ ...form });
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Started':
        return 'status-started';
      case 'Completed':
        return 'status-completed';
      case 'Delayed':
        return 'status-delayed';
      default:
        return '';
    }
  }

  getStatusText(status: string): string {
    return status || 'Unknown';
  }

  showPopup(message: string): void {
    window.alert(message);
  }

  goBack(): void {
    this.router.navigate(['/operations/executions']);
  }
}
