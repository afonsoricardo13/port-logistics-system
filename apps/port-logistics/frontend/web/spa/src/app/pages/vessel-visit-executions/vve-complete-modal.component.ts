import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { VesselVisitExecutionService, VesselVisitExecutionDTO, CompleteVveDTO } from '../../services/oem/vessel-visit-execution.service';

@Component({
  selector: 'app-vve-complete-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './vve-complete-modal.component.html',
  styleUrl: './vve-complete-modal.component.scss'
})
export class VveCompleteModalComponent implements OnInit {
  @Input() vve!: VesselVisitExecutionDTO;
  @Output() completed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  completeForm!: FormGroup;
  loading = false;
  error = '';
  success = '';

  constructor(
    private fb: FormBuilder,
    private vveService: VesselVisitExecutionService
  ) {}

  ngOnInit(): void {
    this.completeForm = this.fb.group({
      actualUnberthTime: ['', Validators.required],
      actualDepartureTime: ['', Validators.required],
      notes: ['', Validators.maxLength(500)]
    });

    // Pre-fill with current time if available
    const now = new Date().toISOString().slice(0, 16);
    this.completeForm.patchValue({
      actualUnberthTime: now,
      actualDepartureTime: now
    });
  }

  onSubmit(): void {
    if (this.completeForm.invalid) {
      this.completeForm.markAllAsTouched();
      this.error = 'Please fill in all required fields correctly.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.success = '';

    const formValue = this.completeForm.value;
    const dto: CompleteVveDTO = {
      actualUnberthTime: new Date(formValue.actualUnberthTime).toISOString(),
      actualDepartureTime: new Date(formValue.actualDepartureTime).toISOString(),
      notes: formValue.notes || undefined
    };

    this.vveService.complete(this.vve.id!, dto).subscribe({
      next: () => {
        this.success = '✅ Vessel Visit Execution completed successfully!';
        this.loading = false;
        
        setTimeout(() => {
          this.completed.emit();
        }, 1500);
      },
      error: (err: any) => {
        console.error('❌ Error completing VVE:', err);
        this.error = err.error?.message || 'Failed to complete Vessel Visit Execution.';
        this.loading = false;
      }
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  formatDate(date: string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString();
  }
}
