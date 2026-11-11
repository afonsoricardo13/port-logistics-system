import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { QualificationService, CreateQualificationDTO, UpdateQualificationDTO } from '../../services/qualification.service';

/**
 * Qualification Form Component
 * 
 * US 2.2.13: Create and edit qualifications
 * In edit mode, only requiredNumber can be updated (qualificationName is immutable)
 */
@Component({
  selector: 'app-qualification-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './qualification-form.component.html',
  styleUrls: ['./qualification-form.component.scss']
})
export class QualificationFormComponent implements OnInit {
  qualificationForm: FormGroup;
  isEditMode: boolean = false;
  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';
  originalQualificationName?: string;

  constructor(
    private fb: FormBuilder,
    private qualificationService: QualificationService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.qualificationForm = this.fb.group({
      qualificationName: ['', [
        Validators.required,
        Validators.minLength(3),
        Validators.maxLength(100)
      ]],
      requiredNumber: [1, [
        Validators.required,
        Validators.min(1),
        Validators.pattern(/^\d+$/)
      ]]
    });
  }

  ngOnInit(): void {
    const qualificationName = this.route.snapshot.paramMap.get('name');
    
    if (qualificationName) {
      this.isEditMode = true;
      this.originalQualificationName = qualificationName;
      this.loadQualification(qualificationName);
      // Disable qualificationName field in edit mode (immutable)
      this.qualificationForm.get('qualificationName')?.disable();
    }
  }

  /**
   * Load qualification data for editing
   */
  loadQualification(qualificationName: string): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.qualificationService.getByName(qualificationName).subscribe({
      next: (data) => {
        this.qualificationForm.patchValue({
          qualificationName: data.qualificationName,
          requiredNumber: data.requiredNumber
        });
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading qualification:', error);
        this.errorMessage = 'Failed to load qualification. Please try again.';
        this.isLoading = false;
      }
    });
  }

  /**
   * Submit the form (create or update)
   */
  onSubmit(): void {
    if (this.qualificationForm.invalid) {
      this.markFormGroupTouched(this.qualificationForm);
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.isLoading = true;

    if (this.isEditMode && this.originalQualificationName) {
      this.updateQualification();
    } else {
      this.createQualification();
    }
  }

  /**
   * Create a new qualification
   */
  private createQualification(): void {
    const dto: CreateQualificationDTO = {
      qualificationName: this.qualificationForm.get('qualificationName')?.value,
      requiredNumber: this.qualificationForm.get('requiredNumber')?.value
    };

    this.qualificationService.create(dto).subscribe({
      next: () => {
        this.successMessage = 'Qualification created successfully!';
        this.isLoading = false;
        setTimeout(() => {
          this.router.navigate(['/qualifications']);
        }, 1500);
      },
      error: (error) => {
        console.error('Error creating qualification:', error);
        this.errorMessage = error.error?.message || 'Failed to create qualification. Please try again.';
        this.isLoading = false;
      }
    });
  }

  /**
   * Update an existing qualification
   * Only requiredNumber can be updated
   */
  private updateQualification(): void {
    if (!this.originalQualificationName) return;

    const dto: UpdateQualificationDTO = {
      requiredNumber: this.qualificationForm.get('requiredNumber')?.value
    };

    this.qualificationService.update(this.originalQualificationName, dto).subscribe({
      next: () => {
        this.successMessage = 'Qualification updated successfully!';
        this.isLoading = false;
        setTimeout(() => {
          this.router.navigate(['/qualifications']);
        }, 1500);
      },
      error: (error) => {
        console.error('Error updating qualification:', error);
        this.errorMessage = error.error?.message || 'Failed to update qualification. Please try again.';
        this.isLoading = false;
      }
    });
  }

  /**
   * Cancel and navigate back to list
   */
  onCancel(): void {
    this.router.navigate(['/qualifications']);
  }

  /**
   * Mark all form controls as touched to show validation errors
   */
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  /**
   * Check if a form control has errors and has been touched
   */
  hasError(controlName: string): boolean {
    const control = this.qualificationForm.get(controlName);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  /**
   * Get error message for a form control
   */
  getErrorMessage(controlName: string): string {
    const control = this.qualificationForm.get(controlName);
    
    if (!control || !control.errors) return '';

    if (control.errors['required']) {
      return `${controlName === 'qualificationName' ? 'Qualification name' : 'Required number'} is required`;
    }
    
    if (control.errors['minlength']) {
      return `Minimum length is ${control.errors['minlength'].requiredLength} characters`;
    }
    
    if (control.errors['maxlength']) {
      return `Maximum length is ${control.errors['maxlength'].requiredLength} characters`;
    }
    
    if (control.errors['min']) {
      return `Minimum value is ${control.errors['min'].min}`;
    }
    
    if (control.errors['pattern']) {
      return 'Please enter a valid number';
    }

    return 'Invalid value';
  }
}
