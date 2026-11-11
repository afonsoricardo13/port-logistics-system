import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { VesselVisitExecutionService, VesselVisitExecutionDTO } from '../../../services/oem/vessel-visit-execution.service';
import { DockManagementService, DockDTO } from '../../../services/dock-management.service';
import { VesselVisitNotificationService, VesselVisitNotificationDTO } from '../../../services/vessel-visit-notification.service';

@Component({
  selector: 'app-vve-berthing',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './vve-berthing.component.html',
  styleUrl: './vve-berthing.component.scss'
})
export class VveBerthingComponent implements OnInit {
  berthingForm!: FormGroup;
  loading = false;
  error = '';
  success = '';
  vveId: string | null = null;
  vve: VesselVisitExecutionDTO | null = null;
  vvn: VesselVisitNotificationDTO | null = null;
  docks: DockDTO[] = [];

  constructor(
    private fb: FormBuilder,
    private vveService: VesselVisitExecutionService,
    private dockService: DockManagementService,
    private vvnService: VesselVisitNotificationService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.vveId = this.route.snapshot.paramMap.get('id');
    if (!this.vveId) {
      this.error = 'No VVE ID provided';
      return;
    }

    this.initForm();
    this.loadDocks();
    this.loadVve();
  }

  private initForm(): void {
    const now = new Date();
    const localDateTime = new Date(now.getTime() - now.getTimezoneOffset() * 60000)
      .toISOString()
      .slice(0, 16);

    this.berthingForm = this.fb.group({
      actualBerthTime: [localDateTime, [Validators.required]],
      dockUsed: ['', [Validators.required]]
    });
  }

  private loadDocks(): void {
    this.dockService.getAll().subscribe({
      next: (docks: DockDTO[]) => this.docks = docks,
      error: (err: any) => console.error('Error loading docks', err)
    });
  }

  private loadVve(): void {
    if (!this.vveId) return;
    this.loading = true;
    
    this.vveService.getAll().subscribe({
      next: (vves: VesselVisitExecutionDTO[]) => {
        this.vve = vves.find((v: VesselVisitExecutionDTO) => v.id === this.vveId) || null;
        this.loading = false;
        if (!this.vve) {
          this.error = 'VVE not found';
        } else if (this.vve.status !== 'InProgress') {
          this.error = 'VVE is not In Progress';
        } else {
            if (this.vve.vvnId) {
              this.loadVvn(Number(this.vve.vvnId));
            }

            if (this.vve.actualBerthTime) {
                 const date = new Date(this.vve.actualBerthTime);
                 const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
                    .toISOString().slice(0, 16);
                 this.berthingForm.patchValue({ actualBerthTime: localDate });
            }
            if (this.vve.dockUsed) {
                this.berthingForm.patchValue({ dockUsed: this.vve.dockUsed });
            }
        }
      },
      error: (err: any) => {
        this.error = 'Error loading VVE';
        this.loading = false;
      }
    });
  }

  private loadVvn(id: number): void {
    this.vvnService.getById(id).subscribe({
      next: (vvn) => {
        this.vvn = vvn;
      },
      error: (err) => console.error('Error loading VVN', err)
    });
  }

  onCancel(): void {
    this.router.navigate(['/operations/executions']);
  }

  onSubmit(): void {
    if (this.berthingForm.invalid || !this.vveId) return;

    this.loading = true;
    this.error = '';
    this.success = '';

    const dto = {
      actualBerthTime: new Date(this.berthingForm.value.actualBerthTime).toISOString(),
      dockUsed: this.berthingForm.value.dockUsed
    };

    this.vveService.updateBerthing(this.vveId, dto).subscribe({
      next: (updated: VesselVisitExecutionDTO) => {
        this.success = 'Berthing information updated successfully!';
        this.loading = false;
        setTimeout(() => this.router.navigate(['/operations/executions']), 2000);
      },
      error: (err: any) => {
        this.error = err.error?.message || 'Failed to update berthing info';
        this.loading = false;
      }
    });
  }
}
