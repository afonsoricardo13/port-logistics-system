import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { OperatingStaffService, CreateOperatingStaffDTO, UpdateOperatingStaffDTO, OperatingStaffDTO } from '../../../services/operating-staff.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-staff-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './staff-form.component.html',
  styleUrl: './staff-form.component.scss'
})
export class StaffFormComponent implements OnInit {
  private operatingStaffService = inject(OperatingStaffService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);

  isEditMode: boolean = false;
  mecanographicNumber: string = '';
  loading: boolean = false;
  error: string | null = null;
  successMessage: string | null = null;
  submitting: boolean = false;

  // Form fields
  formData = {
    mecanographicNumber: '',
    name: '',
    email: '',
    phoneNumber: '',
    qualifications: [] as string[],
    daysPattern: 'Mon-Fri',
    customDays: '',
    startTime: '',
    endTime: '',
    status: 'Available'
  };

  // Temp fields for adding qualifications
  newQualification: string = '';

  ngOnInit(): void {
    // Check route parameter to determine create vs edit
    this.route.params.subscribe(params => {
      if (params['number']) {
        this.isEditMode = true;
        this.mecanographicNumber = params['number'];
        this.loadStaffMember();
      }
    });

    // Check permissions
    if (!this.canManageStaff()) {
      this.error = 'You do not have permission to manage operating staff.';
    }
  }

  loadStaffMember(): void {
    if (!this.mecanographicNumber) return;

    this.loading = true;
    this.error = null;

    this.operatingStaffService.getByMecanographicNumber(this.mecanographicNumber).subscribe({
      next: (staff: OperatingStaffDTO) => {
        // Parse operational window format "Mon-Fri 08:00-16:00" or "HH:mm - HH:mm"
        const { daysPattern, customDays, startTime, endTime } = this.parseOperationalWindow(staff.operationalWindow);
        
        this.formData = {
          mecanographicNumber: staff.mecanographicNumber,
          name: staff.name,
          email: staff.email,
          phoneNumber: staff.phoneNumber,
          qualifications: [...staff.qualifications],
          daysPattern,
          customDays,
          startTime,
          endTime,
          status: staff.status
        };
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading staff member:', err);
        this.error = err.error?.message || 'Failed to load staff member. Please try again.';
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    // Clear messages
    this.error = null;
    this.successMessage = null;

    // Validate form
    if (!this.validateForm()) {
      return;
    }

    this.submitting = true;

    if (this.isEditMode) {
      this.updateStaff();
    } else {
      this.createStaff();
    }
  }

  createStaff(): void {
    const createDto: CreateOperatingStaffDTO = {
      mecanographicNumber: this.formData.mecanographicNumber.trim(),
      name: this.formData.name.trim(),
      email: this.formData.email.trim(),
      phoneNumber: this.formData.phoneNumber.trim(),
      qualifications: this.formData.qualifications,
      operationalWindow: this.buildOperationalWindow()
    };

    this.operatingStaffService.create(createDto).subscribe({
      next: (staff: OperatingStaffDTO) => {
        this.successMessage = `Operating staff member ${staff.name} created successfully!`;
        this.submitting = false;
        
        // Redirect to list after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/operating-staff']);
        }, 2000);
      },
      error: (err) => {
        console.error('Error creating staff:', err);
        // Handle errors array from backend
        if (err.error?.errors && Array.isArray(err.error.errors)) {
          this.error = err.error.errors.join('. ');
        } else {
          this.error = err.error?.message || 'Failed to create operating staff. Please try again.';
        }
        this.submitting = false;
      }
    });
  }

  updateStaff(): void {
    const updateDto: UpdateOperatingStaffDTO = {
      name: this.formData.name.trim(),
      email: this.formData.email.trim(),
      phoneNumber: this.formData.phoneNumber.trim(),
      qualifications: this.formData.qualifications,
      operationalWindow: this.buildOperationalWindow()
    };

    this.operatingStaffService.update(this.mecanographicNumber, updateDto).subscribe({
      next: (staff: OperatingStaffDTO) => {
        this.successMessage = `Operating staff member ${staff.name} updated successfully!`;
        this.submitting = false;
        
        // Redirect to list after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/operating-staff']);
        }, 2000);
      },
      error: (err) => {
        console.error('Error updating staff:', err);
        // Handle errors array from backend
        if (err.error?.errors && Array.isArray(err.error.errors)) {
          this.error = err.error.errors.join('. ');
        } else {
          this.error = err.error?.message || 'Failed to update operating staff. Please try again.';
        }
        this.submitting = false;
      }
    });
  }

  validateForm(): boolean {
    // Basic client-side validation - backend will do full validation
    if (!this.formData.name.trim()) {
      this.error = 'Name is required.';
      return false;
    }

    if (!this.isEditMode && !this.formData.mecanographicNumber.trim()) {
      this.error = 'Mecanographic number is required.';
      return false;
    }

    if (this.formData.qualifications.length === 0) {
      this.error = 'At least one qualification is required.';
      return false;
    }

    return true;
  }

  addQualification(): void {
    const qualification = this.newQualification.trim();
    
    if (!qualification) {
      return;
    }

    // Check for duplicates
    if (this.formData.qualifications.includes(qualification)) {
      this.error = 'This qualification has already been added.';
      return;
    }

    this.formData.qualifications.push(qualification);
    this.newQualification = '';
    this.error = null;
  }

  removeQualification(index: number): void {
    this.formData.qualifications.splice(index, 1);
  }

  onCancel(): void {
    if (confirm('Are you sure you want to cancel? Any unsaved changes will be lost.')) {
      this.router.navigate(['/operating-staff']);
    }
  }

  canManageStaff(): boolean {
    const userRole = this.authService.getUserRole();
    return userRole === 'LogisticsOperator';
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit Operating Staff Member' : 'Register Operating Staff Member';
  }

  get submitButtonText(): string {
    return this.isEditMode ? 'Update' : 'Create';
  }

  parseOperationalWindow(window: string): { daysPattern: string; customDays: string; startTime: string; endTime: string } {
    // Try to parse format like "Mon-Fri 08:00-16:00" or "Monday-Friday 08:00-16:00"
    const fullPatternMatch = window.match(/^([A-Za-z\-]+)\s+(\d{2}:\d{2})[-–](\d{2}:\d{2})$/);
    
    if (fullPatternMatch) {
      const [, days, start, end] = fullPatternMatch;
      
      // Map common patterns
      const normalizedDays = days.replace('Monday-Friday', 'Mon-Fri')
                                  .replace('Monday-Saturday', 'Mon-Sat')
                                  .replace('Monday-Sunday', 'Mon-Sun')
                                  .replace('Saturday-Sunday', 'Sat-Sun');
      
      const knownPatterns = ['Mon-Fri', 'Mon-Sat', 'Mon-Sun', 'Sat-Sun'];
      const daysPattern = knownPatterns.includes(normalizedDays) ? normalizedDays : 'Custom';
      const customDays = daysPattern === 'Custom' ? days : '';
      
      return { daysPattern, customDays, startTime: start, endTime: end };
    }
    
    // Fallback: try to parse "HH:mm - HH:mm" (old format)
    const simpleMatch = window.match(/^(\d{2}:\d{2})\s*[-–]\s*(\d{2}:\d{2})$/);
    if (simpleMatch) {
      const [, start, end] = simpleMatch;
      return { daysPattern: 'Mon-Fri', customDays: '', startTime: start, endTime: end };
    }
    
    // Default fallback
    return { daysPattern: 'Mon-Fri', customDays: '', startTime: '', endTime: '' };
  }

  buildOperationalWindow(): string {
    const days = this.formData.daysPattern === 'Custom' 
      ? this.formData.customDays.trim() 
      : this.formData.daysPattern;
    
    return `${days} ${this.formData.startTime}-${this.formData.endTime}`;
  }

  getOperationalWindowPreview(): string {
    if (!this.formData.startTime || !this.formData.endTime) {
      return 'Please fill in all fields';
    }
    return this.buildOperationalWindow();
  }
}
