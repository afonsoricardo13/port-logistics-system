import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { StorageAreaManagementService, StorageAreaDTO, ServedDockDTO } from '../../services/storage-area-management.service';
import { DockManagementService, DockDTO } from '../../services/dock-management.service';

@Component({
  selector: 'app-storage-areas-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './storage-areas-form.component.html',
  styleUrls: ['./storage-areas-form.component.scss']
})
export class StorageAreasFormComponent implements OnInit {
  form!: FormGroup;
  identifier: string | null = null;
  isEditMode = false;
  loading = false;
  submitting = false;
  error: string | null = null;
  successMessage: string | null = null;

  storageTypes = ['Yard', 'Warehouse'];
  availableDocks: DockDTO[] = [];

  constructor(
    private fb: FormBuilder,
    private service: StorageAreaManagementService,
    private dockService: DockManagementService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadDocks();
    
    const id = this.route.snapshot.paramMap.get('identifier');
    if (id && id !== 'new') {
      this.identifier = id;
      this.isEditMode = true;
      this.loadStorageArea();
    }
  }

  loadDocks(): void {
    this.dockService.getAll().subscribe({
      next: (docks) => {
        this.availableDocks = docks;
      },
      error: (err) => {
        console.error('Error loading docks:', err);
      }
    });
  }

  initForm(): void {
    this.form = this.fb.group({
      identifier: ['', [Validators.required, Validators.pattern(/^[A-Z0-9-]+$/)]],
      type: ['', Validators.required],
      location: ['', Validators.required],
      maxCapacityTeu: [0, [Validators.required, Validators.min(0)]],
      currentOccupancyTeu: [0, [Validators.required, Validators.min(0)]],
      servedDocks: this.fb.array([])
    });

    // Add validation to ensure occupancy doesn't exceed capacity
    this.form.get('currentOccupancyTeu')?.valueChanges.subscribe(() => this.validateCapacity());
    this.form.get('maxCapacityTeu')?.valueChanges.subscribe(() => this.validateCapacity());
  }

  get servedDocksArray(): FormArray {
    return this.form.get('servedDocks') as FormArray;
  }

  loadStorageArea(): void {
    if (!this.identifier) return;

    this.loading = true;
    this.error = null;

    this.service.getByIdentifier(this.identifier).subscribe({
      next: (data: StorageAreaDTO) => {
        this.form.patchValue({
          identifier: data.identifier,
          type: data.type,
          location: data.location,
          maxCapacityTeu: data.maxCapacityTeu,
          currentOccupancyTeu: data.currentOccupancyTeu
        });

        // Load served docks
        data.servedDocks.forEach(dock => this.addServedDock(dock));

        // Disable identifier in edit mode
        this.form.get('identifier')?.disable();
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load storage area';
        console.error('Error loading storage area:', err);
        this.loading = false;
      }
    });
  }

  addServedDock(dock?: ServedDockDTO): void {
    const dockGroup = this.fb.group({
      dockCode: [dock?.dockCode || '', [Validators.required, Validators.pattern(/^[A-Z0-9-]+$/)]],
      distanceMeters: [dock?.distanceMeters || 0, [Validators.required, Validators.min(0)]]
    });
    this.servedDocksArray.push(dockGroup);
  }

  removeServedDock(index: number): void {
    this.servedDocksArray.removeAt(index);
  }

  validateCapacity(): void {
    const current = this.form.get('currentOccupancyTeu')?.value;
    const max = this.form.get('maxCapacityTeu')?.value;

    if (current > max) {
      this.form.get('currentOccupancyTeu')?.setErrors({ exceedsCapacity: true });
    }
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error = 'Please fill all required fields correctly';
      return;
    }

    this.submitting = true;
    this.error = null;
    this.successMessage = null;

    // Enable identifier temporarily for submission
    this.form.get('identifier')?.enable();

    const dto: StorageAreaDTO = {
      ...this.form.value,
      servedDocks: this.servedDocksArray.value
    };

    const operation = this.isEditMode && this.identifier
      ? this.service.update(this.identifier, dto)
      : this.service.create(dto);

    operation.subscribe({
      next: () => {
        this.successMessage = this.isEditMode 
          ? 'Storage area updated successfully' 
          : 'Storage area created successfully';
        this.submitting = false;
        
        setTimeout(() => this.router.navigate(['/storage-areas']), 1500);
      },
      error: (err) => {
        let errorMsg = 'Failed to save storage area';
        if (err.error) {
          if (Array.isArray(err.error)) {
            errorMsg = err.error.join('; ');
          } else if (typeof err.error === 'string') {
            errorMsg = err.error;
          } else if (err.error.message) {
            errorMsg = err.error.message;
          }
        }
        
        this.error = errorMsg;
        this.submitting = false;
        
        // Re-disable identifier in edit mode
        if (this.isEditMode) {
          this.form.get('identifier')?.disable();
        }
        
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/storage-areas']);
  }

  get f() { return this.form.controls; }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }
}
