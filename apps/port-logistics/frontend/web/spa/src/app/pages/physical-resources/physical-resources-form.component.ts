import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { PhysicalResourceManagementService, PhysicalResourceDTO } from '../../services/physical-resource-management.service';

@Component({
  selector: 'app-physical-resources-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './physical-resources-form.component.html',
  styleUrl: './physical-resources-form.component.scss'
})
export class PhysicalResourcesFormComponent implements OnInit {
  resourceForm!: FormGroup;
  isEditMode = false;
  originalCode = '';
  loading = false;
  error = '';
  success = '';

  availableTypes = ['STS_CRANE', 'YARD_CRANE', 'TRUCK'];
  availableStatuses = ['Active', 'Inactive', 'UnderMaintenance'];
  availableQualifications = ['STS_CRANE_OPERATOR', 'TRUCK_DRIVER', 'YARD_CRANE_OPERATOR'];

  constructor(
    private fb: FormBuilder,
    private resourceService: PhysicalResourceManagementService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.checkEditMode();
  }

  /**
   * Initialize form with validators
   */
  private initForm(): void {
    this.resourceForm = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(20)]],
      description: ['', [Validators.required, Validators.maxLength(255)]],
      type: ['', Validators.required],
      capacity: [1, [Validators.required, Validators.min(1)]],
      assignedArea: [''],
      status: ['Active', Validators.required],
      setupTimeMinutes: [null, Validators.min(0)],
      qualificationRequirementIds: this.fb.array([]),
      operationalWindowStart: [''],
      operationalWindowEnd: [''],
      capacityPerHour: [null, Validators.min(1)],
      averageSpeedKmh: [null, Validators.min(1)]
    });

    // Add conditional validators based on type
    this.resourceForm.get('type')?.valueChanges.subscribe(type => {
      this.updateConditionalValidators(type);
    });
  }

  private updateConditionalValidators(type: string): void {
    const operationalWindowStart = this.resourceForm.get('operationalWindowStart');
    const operationalWindowEnd = this.resourceForm.get('operationalWindowEnd');
    const capacityPerHour = this.resourceForm.get('capacityPerHour');
    const averageSpeedKmh = this.resourceForm.get('averageSpeedKmh');

    // Clear all validators first
    operationalWindowStart?.clearValidators();
    operationalWindowEnd?.clearValidators();
    capacityPerHour?.clearValidators();
    averageSpeedKmh?.clearValidators();

    if (type === 'STS_CRANE' || type === 'YARD_CRANE') {
      // Cranes require operational window and capacity per hour
      operationalWindowStart?.setValidators([Validators.required]);
      operationalWindowEnd?.setValidators([Validators.required]);
      capacityPerHour?.setValidators([Validators.required, Validators.min(1)]);
    } else if (type === 'TRUCK') {
      // Trucks require operational window and average speed
      operationalWindowStart?.setValidators([Validators.required]);
      operationalWindowEnd?.setValidators([Validators.required]);
      averageSpeedKmh?.setValidators([Validators.required, Validators.min(1)]);
    }

    operationalWindowStart?.updateValueAndValidity();
    operationalWindowEnd?.updateValueAndValidity();
    capacityPerHour?.updateValueAndValidity();
    averageSpeedKmh?.updateValueAndValidity();
  }

  /**
   * Check if editing existing resource
   */
  private checkEditMode(): void {
    const code = this.route.snapshot.paramMap.get('code');
    if (code) {
      this.isEditMode = true;
      this.originalCode = code;
      this.resourceForm.get('code')?.disable(); // Code cannot be changed
      this.loadResource(code);
    }
  }

  /**
   * Load existing resource for editing
   */
  private loadResource(code: string): void {
    this.loading = true;
    this.resourceService.getByCode(code).subscribe({
      next: (resource) => {
        let windowStart = '';
        let windowEnd = '';
        if ((resource as any).operationalWindow) {
          const match = (resource as any).operationalWindow.match(/Mon-Sun (\d{2}:\d{2})-(\d{2}:\d{2})/);
          if (match) {
            windowStart = match[1];
            windowEnd = match[2];
          }
        }

        this.resourceForm.patchValue({
          code: resource.code,
          description: resource.description,
          type: resource.type,
          capacity: resource.capacity,
          assignedArea: resource.assignedArea || '',
          status: resource.status || 'Active',
          setupTimeMinutes: resource.setupTimeMinutes,
          operationalWindowStart: windowStart,
          operationalWindowEnd: windowEnd,
          capacityPerHour: (resource as any).capacityPerHour || null,
          averageSpeedKmh: (resource as any).averageSpeedKmH || null
        });

        // Load qualification IDs
        if (resource.qualificationRequirementIds) {
          resource.qualificationRequirementIds.forEach(id => {
            this.addQualificationId(id);
          });
        }

        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading resource:', err);
        this.error = 'Failed to load resource.';
        this.loading = false;
      }
    });
  }

  /**
   * Get qualification IDs form array
   */
  get qualificationIdsArray(): FormArray {
    return this.resourceForm.get('qualificationRequirementIds') as FormArray;
  }

  /**
   * Add qualification ID to array
   */
  addQualificationId(value: string = ''): void {
    this.qualificationIdsArray.push(this.fb.control(value || '', Validators.required));
  }

  /**
   * Remove qualification ID from array
   */
  removeQualificationId(index: number): void {
    this.qualificationIdsArray.removeAt(index);
  }

  /**
   * Submit form
   */
  onSubmit(): void {
    console.log('Form submitted', this.resourceForm.value);
    console.log('Form valid:', this.resourceForm.valid);
    console.log('Form errors:', this.getFormValidationErrors());
    
    if (this.resourceForm.invalid) {
      this.resourceForm.markAllAsTouched();
      this.error = 'Please fill in all required fields correctly.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.success = '';

    const formValue = this.resourceForm.getRawValue();
    const dto: any = {
      code: formValue.code,
      description: formValue.description,
      type: formValue.type,
      capacity: formValue.capacity,
      assignedArea: formValue.assignedArea || undefined,
      status: formValue.status,
      setupTimeMinutes: formValue.setupTimeMinutes || undefined,
      qualificationRequirementIds: formValue.qualificationRequirementIds.filter((id: string) => id.trim() !== ''),
      changedBy: 'CurrentUser'
    };

    // Add conditional fields based on type
    if (formValue.type === 'STS_CRANE' || formValue.type === 'YARD_CRANE') {
      dto.operationalWindow = `Mon-Sun ${formValue.operationalWindowStart}-${formValue.operationalWindowEnd}`;
      dto.capacityPerHour = formValue.capacityPerHour;
    } else if (formValue.type === 'TRUCK') {
      dto.operationalWindow = `Mon-Sun ${formValue.operationalWindowStart}-${formValue.operationalWindowEnd}`;
      dto.averageSpeedKmH = formValue.averageSpeedKmh;
    }

    if (this.isEditMode) {
      this.resourceService.update(this.originalCode, dto).subscribe({
        next: () => {
          this.success = 'Resource updated successfully!';
          this.loading = false;
          
          setTimeout(() => {
            this.router.navigate(['/physical-resources']);
          }, 1500);
        },
        error: (err: any) => {
          console.error('Error saving resource:', err);
          this.error = this.extractError(err);
          this.loading = false;
        }
      });
    } else {
      this.resourceService.create(dto).subscribe({
        next: () => {
          this.success = 'Resource created successfully!';
          this.loading = false;
          
          setTimeout(() => {
            this.router.navigate(['/physical-resources']);
          }, 1500);
        },
        error: (err: any) => {
          console.error('Error saving resource:', err);
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
    return 'Failed to save resource.';
  }

  /**
   * Cancel and go back
   */
  cancel(): void {
    this.router.navigate(['/physical-resources']);
  }

  /**
   * Get form validation errors for debugging
   */
  getFormValidationErrors(): any {
    const errors: any = {};
    Object.keys(this.resourceForm.controls).forEach(key => {
      const control = this.resourceForm.get(key);
      if (control && control.errors) {
        errors[key] = control.errors;
      }
    });
    return errors;
  }
}
