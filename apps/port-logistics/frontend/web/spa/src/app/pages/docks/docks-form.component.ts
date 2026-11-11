import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { DockManagementService, DockDTO } from '../../services/dock-management.service';
import { VesselTypeManagementService, VesselTypeDTO } from '../../services/vessel-type-management.service';

@Component({
  selector: 'app-docks-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './docks-form.component.html',
  styleUrl: './docks-form.component.scss'
})
export class DocksFormComponent implements OnInit {
  dockForm!: FormGroup;
  isEditMode = false;
  originalCode = '';
  loading = false;
  error = '';
  success = '';
  availableVesselTypes: VesselTypeDTO[] = [];

  constructor(
    private fb: FormBuilder,
    private dockService: DockManagementService,
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
    this.dockForm = this.fb.group({
      code: ['', [Validators.required, Validators.maxLength(50)]],
      nameOrNumber: ['', [Validators.required, Validators.maxLength(100)]],
      location: ['', [Validators.required, Validators.maxLength(200)]],
      length: [0, [Validators.required, Validators.min(0.01)]],
      depth: [0, [Validators.required, Validators.min(0.01)]],
      maxDraft: [0, [Validators.required, Validators.min(0.01)]],
      allowedVesselTypes: this.fb.array([], Validators.required)
    });
  }

  /**
   * Check if editing existing dock
   */
  private checkEditMode(): void {
    const code = this.route.snapshot.paramMap.get('code');
    if (code) {
      this.isEditMode = true;
      this.originalCode = code;
      this.dockForm.get('code')?.disable(); // Code cannot be changed
      this.loadDock(code);
    }
  }

  /**
   * Load existing dock for editing
   */
  private loadDock(code: string): void {
    this.loading = true;
    this.dockService.getByCode(code).subscribe({
      next: (dock) => {
        this.dockForm.patchValue({
          code: dock.code,
          nameOrNumber: dock.nameOrNumber,
          location: dock.location,
          length: dock.length,
          depth: dock.depth,
          maxDraft: dock.maxDraft
        });

        // Load vessel types
        if (dock.allowedVesselTypes) {
          dock.allowedVesselTypes.forEach(type => {
            this.addVesselType(type);
          });
        }

        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading dock:', err);
        this.error = 'Failed to load dock.';
        this.loading = false;
      }
    });
  }

  /**
   * Get vessel types form array
   */
  get vesselTypesArray(): FormArray {
    return this.dockForm.get('allowedVesselTypes') as FormArray;
  }

  /**
   * Add vessel type to array
   */
  addVesselType(value: string = ''): void {
    this.vesselTypesArray.push(this.fb.control(value, Validators.required));
  }

  /**
   * Remove vessel type from array
   */
  removeVesselType(index: number): void {
    this.vesselTypesArray.removeAt(index);
  }

  /**
   * Submit form
   */
  onSubmit(): void {
    console.log('Form submitted', this.dockForm.value);
    console.log('Form valid:', this.dockForm.valid);
    
    if (this.dockForm.invalid) {
      this.dockForm.markAllAsTouched();
      this.error = 'Please fill in all required fields correctly.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.success = '';

    const formValue = this.dockForm.getRawValue();
    const dto: DockDTO = {
      code: formValue.code,
      nameOrNumber: formValue.nameOrNumber,
      location: formValue.location,
      length: formValue.length,
      depth: formValue.depth,
      maxDraft: formValue.maxDraft,
      allowedVesselTypes: formValue.allowedVesselTypes.filter((type: string) => type.trim() !== '')
    };

    if (this.isEditMode) {
      this.dockService.update(this.originalCode, dto).subscribe({
        next: () => {
          this.success = 'Dock updated successfully!';
          this.loading = false;
          
          setTimeout(() => {
            this.router.navigate(['/docks']);
          }, 1500);
        },
        error: (err: any) => {
          console.error('Error saving dock:', err);
          this.error = this.extractError(err);
          this.loading = false;
        }
      });
    } else {
      this.dockService.create(dto).subscribe({
        next: () => {
          this.success = 'Dock created successfully!';
          this.loading = false;
          
          setTimeout(() => {
            this.router.navigate(['/docks']);
          }, 1500);
        },
        error: (err: any) => {
          console.error('Error saving dock:', err);
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
    return 'Failed to save dock.';
  }

  /**
   * Cancel and go back
   */
  cancel(): void {
    this.router.navigate(['/docks']);
  }
}
