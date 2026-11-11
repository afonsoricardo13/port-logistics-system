import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { VesselTypeManagementService, VesselTypeDTO } from '../../services/vessel-type-management.service';

/**
 * Vessel Type Form Component
 * 
 * US 2.2.1: Create and update vessel types
 */
@Component({
  selector: 'app-vessel-types-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './vessel-types-form.component.html',
  styleUrls: ['./vessel-types-form.component.scss']
})
export class VesselTypesFormComponent implements OnInit {
  vesselTypeForm: FormGroup;
  isEditMode = false;
  originalName = '';
  isSubmitting = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private vesselTypeService: VesselTypeManagementService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.vesselTypeForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['', [Validators.required, Validators.minLength(5)]],
      capacity: [0, [Validators.required, Validators.min(1)]],
      maxRows: [0, [Validators.required, Validators.min(1)]],
      maxBays: [0, [Validators.required, Validators.min(1)]],
      maxTiers: [0, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void {
    const name = this.route.snapshot.paramMap.get('name');
    if (name) {
      this.isEditMode = true;
      this.originalName = name;
      this.loadVesselType(name);
      // In edit mode, name should be readonly
      this.vesselTypeForm.get('name')?.disable();
    }
  }

  loadVesselType(name: string): void {
    this.vesselTypeService.getByName(name).subscribe({
      next: (data) => {
        this.vesselTypeForm.patchValue(data);
      },
      error: (error) => {
        console.error('Error loading vessel type:', error);
        this.errorMessage = 'Failed to load vessel type. Please try again.';
      }
    });
  }

  onSubmit(): void {
    if (this.vesselTypeForm.invalid) {
      this.vesselTypeForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    // Get form value and ensure name is included even if disabled
    const formValue = {
      ...this.vesselTypeForm.getRawValue()
    };

    const dto: VesselTypeDTO = {
      name: formValue.name,
      description: formValue.description,
      capacity: formValue.capacity,
      maxRows: formValue.maxRows,
      maxBays: formValue.maxBays,
      maxTiers: formValue.maxTiers
    };

    if (this.isEditMode) {
      this.vesselTypeService.update(this.originalName, dto).subscribe({
        next: () => {
          console.log('Vessel type updated successfully');
          this.router.navigate(['/vessel-types']);
        },
        error: (error) => {
          console.error('Error updating vessel type:', error);
          this.errorMessage = error.error?.join(', ') || 'Failed to update vessel type. Please try again.';
          this.isSubmitting = false;
        }
      });
    } else {
      this.vesselTypeService.create(dto).subscribe({
        next: (created) => {
          console.log('Vessel type created successfully:', created);
          this.router.navigate(['/vessel-types']);
        },
        error: (error) => {
          console.error('Error creating vessel type:', error);
          this.errorMessage = error.error?.join(', ') || 'Failed to create vessel type. Please try again.';
          this.isSubmitting = false;
        }
      });
    }
  }
}
