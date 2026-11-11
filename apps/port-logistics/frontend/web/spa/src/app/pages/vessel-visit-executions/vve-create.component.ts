import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { VesselVisitExecutionService, CreateVveDTO } from '../../services/oem/vessel-visit-execution.service';
import { VesselVisitNotificationService, VesselVisitNotificationDTO } from '../../services/vessel-visit-notification.service';

@Component({
  selector: 'app-vve-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './vve-create.component.html',
  styleUrl: './vve-create.component.scss'
})
export class VveCreateComponent implements OnInit {
  vveForm!: FormGroup;
  loading = false;
  error = '';
  success = '';
  
  vvnList: VesselVisitNotificationDTO[] = [];
  loadingVvns = false;
  selectedVvn: VesselVisitNotificationDTO | null = null;

  constructor(
    private fb: FormBuilder,
    private vveService: VesselVisitExecutionService,
    private vvnService: VesselVisitNotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadApprovedVvns();
  }

  /**
   * US 4.1.7: Initialize form
   */
  private initForm(): void {
    const now = new Date();
    const localDateTime = new Date(now.getTime() - now.getTimezoneOffset() * 60000)
      .toISOString()
      .slice(0, 16);

    this.vveForm = this.fb.group({
      vvnId: ['', [Validators.required]],
      actualArrivalTime: [localDateTime, [Validators.required]],
      notes: ['', [Validators.maxLength(500)]]
    });
  }

  /**
   * Load approved VVNs for selection
   */
  private loadApprovedVvns(): void {
    this.loadingVvns = true;
    this.vvnService.getAll().subscribe({
      next: (vvns: VesselVisitNotificationDTO[]) => {
        this.vvnList = vvns.filter((vvn: VesselVisitNotificationDTO) => vvn.status === 'Approved');
        this.loadingVvns = false;
      },
      error: (err: any) => {
        console.error('Error loading VVNs:', err);
        this.error = 'Failed to load vessel visit notifications.';
        this.loadingVvns = false;
      }
    });
  }

  /**
   * When VVN is selected, pre-fill with ETA
   */
  onVvnSelected(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const vvnId = select.value;
    
    this.selectedVvn = this.vvnList.find(vvn => vvn.id?.toString() === vvnId) || null;
    
    if (this.selectedVvn && this.selectedVvn.estimatedTimeOfArrival) {
      const eta = new Date(this.selectedVvn.estimatedTimeOfArrival);
      const localEta = new Date(eta.getTime() - eta.getTimezoneOffset() * 60000)
        .toISOString()
        .slice(0, 16);
      this.vveForm.patchValue({ actualArrivalTime: localEta });
    }
  }

  /**
   * Get selected VVN details for display
   */
  getVvnDetails(): string {
    if (!this.selectedVvn) return '';
    
    const vessel = this.selectedVvn.vesselImoNumber;
    const dock = this.selectedVvn.assignedDockCode || 'Not assigned';
    const eta = this.selectedVvn.estimatedTimeOfArrival 
      ? new Date(this.selectedVvn.estimatedTimeOfArrival).toLocaleString()
      : 'N/A';
    
    return `Vessel IMO: ${vessel} | Dock: ${dock} | ETA: ${eta}`;
  }

  /**
   * US 4.1.7: Submit form to create VVE
   */
  onSubmit(): void {
    if (this.vveForm.invalid) {
      this.vveForm.markAllAsTouched();
      this.error = 'Please fill in all required fields correctly.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.success = '';

    const formValue = this.vveForm.value;
    
    const dto: CreateVveDTO = {
      vvnId: formValue.vvnId?.toString() || '',
      actualArrivalTime: new Date(formValue.actualArrivalTime).toISOString(),
      notes: formValue.notes || undefined
    };

    this.vveService.create(dto).subscribe({
      next: (created: any) => {
        this.success = `✅ Vessel Visit Execution created successfully! ID: ${created.id}`;
        this.loading = false;
        
        console.log('✅ US 4.1.7: VVE created:', created);
        
        setTimeout(() => {
          this.router.navigate(['/operations/executions']);
        }, 2000);
      },
      error: (err: any) => {
        console.error('❌ Error creating VVE:', err);
        this.error = err.error?.message || 'Failed to create Vessel Visit Execution.';
        this.loading = false;
      }
    });
  }

  /**
   * Cancel and go back
   */
  onCancel(): void {
    this.router.navigate(['/operations/executions']);
  }
}
