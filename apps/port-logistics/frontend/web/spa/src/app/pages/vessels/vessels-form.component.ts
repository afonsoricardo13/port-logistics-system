import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { VesselManagementService, VesselDTO } from '../../services/vessel-management.service';
import { VesselTypeManagementService, VesselTypeDTO } from '../../services/vessel-type-management.service';

@Component({
  selector: 'app-vessels-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './vessels-form.component.html',
  styleUrl: './vessels-form.component.scss'
})
export class VesselsFormComponent implements OnInit {
  vesselForm!: FormGroup;
  isEditMode = false;
  originalImo = '';
  loading = false;
  error = '';
  success = '';
  imoValidationError = '';
  availableVesselTypes: VesselTypeDTO[] = [];

  constructor(
    private fb: FormBuilder,
    private vesselService: VesselManagementService,
    private vesselTypeService: VesselTypeManagementService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadVesselTypes();
    this.checkEditMode();
  }

  private loadVesselTypes(): void {
    this.vesselTypeService.getAll().subscribe({
      next: (types) => {
        this.availableVesselTypes = types;
      },
      error: (err) => {
        console.error('Error loading vessel types:', err);
      }
    });
  }

  /**
   * Initialize form with validators
   */
  private initForm(): void {
    this.vesselForm = this.fb.group({
      imoNumber: ['', [Validators.required, Validators.pattern(/^\d{7}$/)]],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      vesselType: ['', [Validators.required, Validators.maxLength(100)]],
      operator: ['', [Validators.required, Validators.maxLength(200)]]
    });

    // Add IMO validation on blur
    this.vesselForm.get('imoNumber')?.valueChanges.subscribe(value => {
      if (value && value.length === 7) {
        this.validateImoNumber(value);
      } else {
        this.imoValidationError = '';
      }
    });
  }

  /**
   * Check if editing existing vessel
   */
  private checkEditMode(): void {
    const imo = this.route.snapshot.paramMap.get('imo');
    if (imo) {
      this.isEditMode = true;
      this.originalImo = imo;
      this.vesselForm.get('imoNumber')?.disable(); // IMO cannot be changed
      this.loadVessel(imo);
    }
  }

  /**
   * Load existing vessel for editing
   */
  private loadVessel(imo: string): void {
    this.loading = true;
    this.vesselService.getByImo(imo).subscribe({
      next: (vessel) => {
        // Remove IMO prefix if present for display
        let imoDigits = vessel.imoNumber.toUpperCase();
        if (imoDigits.startsWith('IMO')) {
          imoDigits = imoDigits.substring(3).trim();
        }

        this.vesselForm.patchValue({
          imoNumber: imoDigits,
          name: vessel.name,
          vesselType: vessel.vesselType,
          operator: vessel.operator
        });

        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading vessel:', err);
        this.error = 'Failed to load vessel.';
        this.loading = false;
      }
    });
  }

  /**
   * Validate IMO number using check digit algorithm
   */
  validateImoNumber(imo: string): void {
    if (this.vesselService.validateIMO(imo)) {
      this.imoValidationError = '';
    } else {
      this.imoValidationError = 'Invalid IMO number. Check digit does not match.';
    }
  }

  /**
   * Submit form
   */
  onSubmit(): void {
    console.log('Form submitted', this.vesselForm.value);
    console.log('Form valid:', this.vesselForm.valid);
    console.log('IMO validation error:', this.imoValidationError);
    
    if (this.vesselForm.invalid || this.imoValidationError) {
      this.vesselForm.markAllAsTouched();
      this.error = this.imoValidationError || 'Please fill in all required fields correctly.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.success = '';

    const formValue = this.vesselForm.getRawValue();
    const dto: VesselDTO = {
      imoNumber: formValue.imoNumber,
      name: formValue.name,
      vesselType: formValue.vesselType,
      operator: formValue.operator
    };

    if (this.isEditMode) {
      this.vesselService.update(this.originalImo, dto).subscribe({
        next: () => {
          this.success = 'Vessel updated successfully!';
          this.loading = false;
          
          setTimeout(() => {
            this.router.navigate(['/vessels']);
          }, 1500);
        },
        error: (err: any) => {
          console.error('Error saving vessel:', err);
          this.error = this.extractError(err);
          this.loading = false;
        }
      });
    } else {
      this.vesselService.create(dto).subscribe({
        next: () => {
          this.success = 'Vessel created successfully!';
          this.loading = false;
          
          setTimeout(() => {
            this.router.navigate(['/vessels']);
          }, 1500);
        },
        error: (err: any) => {
          console.error('Error saving vessel:', err);
          this.error = this.extractError(err);
          this.loading = false;
        }
      });
    }
  }

  /**
   * Extract error message from response
   */
  private extractError(err: any): string {
    if (err.error) {
      if (Array.isArray(err.error)) {
        return err.error.join(', ');
      }
      if (typeof err.error === 'string') {
        return err.error;
      }
      if (err.error.message) {
        return err.error.message;
      }
      if (err.error.title) {
        return err.error.title;
      }
    }
    return 'Failed to save vessel.';
  }

  /**
   * Cancel and go back
   */
  cancel(): void {
    this.router.navigate(['/vessels']);
  }
}
