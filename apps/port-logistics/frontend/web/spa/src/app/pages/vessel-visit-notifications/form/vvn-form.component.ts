import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormArray } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { VesselVisitNotificationService, VesselVisitNotificationDTO, CrewMemberDTO } from '../../../services/vessel-visit-notification.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-vessel-visit-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './vvn-form.component.html',
  styleUrls: ['./vvn-form.component.scss']
})
export class VvnFormComponent implements OnInit {
  form!: FormGroup;
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;
  isEditMode = false;
  notificationId?: number;
  
  // User context
  userRole = '';
  userEmail = '';
  organizationName = '';

  constructor(
    private fb: FormBuilder,
    private vesselVisitService: VesselVisitNotificationService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.initForm();
    
    // Check if we're in edit mode
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.notificationId = parseInt(id, 10);
      this.loadNotification();
    }
  }

  private async loadUserInfo(): Promise<void> {
    const profile = this.authService.getUserProfile();
    const role = this.authService.getUserRole();
    
    if (profile) {
      this.userEmail = profile.email || '';
    }
    if (role) {
      this.userRole = role;
    }

    if (this.userRole === 'ShippingAgentRepresentative' && this.userEmail) {
      await this.loadOrganization();
    }
  }

  private async loadOrganization(): Promise<void> {
    try {
      const response = await fetch(
        `${this.vesselVisitService['apiUrl']}/api/representatives/by-email/${encodeURIComponent(this.userEmail)}`,
        { headers: { 'X-User-Email': this.userEmail } }
      );
      if (response.ok) {
        const data = await response.json();
        this.organizationName = data.organizationIdentifier || '';
      }
    } catch (error) {
      console.error('Error loading organization:', error);
    }
  }

  private initForm(): void {
    this.form = this.fb.group({
      vesselImoNumber: ['', [Validators.required, Validators.pattern(/^\d{7}$/), this.imoCheckDigitValidator.bind(this)]],
      estimatedTimeOfArrival: ['', Validators.required],
      estimatedTimeOfDeparture: ['', Validators.required],
      captainName: [''],
      crewCount: [null, [Validators.min(1)]],
      preferredDockCode: [''],
      
      // Containers section
      containerIds: this.fb.array([]),
      
      // Crew section (optional - for dangerous cargo)
      crew: this.fb.array([]),
      
      // Safety officers (for dangerous cargo)
      safetyOfficers: this.fb.array([])
    }, { validators: this.dateRangeValidator.bind(this) });
  }

  private dateRangeValidator(group: FormGroup): { [key: string]: any } | null {
    const eta = group.get('estimatedTimeOfArrival')?.value;
    const etd = group.get('estimatedTimeOfDeparture')?.value;

    if (eta && etd) {
      const etaDate = new Date(eta);
      const etdDate = new Date(etd);

      if (etdDate <= etaDate) {
        return { 'invalidDateRange': true };
      }
    }

    return null;
  }

  private imoCheckDigitValidator(control: any): { [key: string]: any } | null {
    const value = control.value;
    if (!value || value.length !== 7) {
      return null; // Let the pattern validator handle this
    }

    // Calculate check digit
    const digits = value.split('').map((d: string) => parseInt(d, 10));
    const multipliers = [7, 6, 5, 4, 3, 2];
    let sum = 0;
    
    for (let i = 0; i < 6; i++) {
      sum += digits[i] * multipliers[i];
    }
    
    const checkDigit = sum % 10;
    const lastDigit = digits[6];
    
    if (checkDigit !== lastDigit) {
      return { 'invalidImoCheckDigit': true };
    }
    
    return null;
  }

  private loadNotification(): void {
    if (!this.notificationId) return;

    this.loading = true;
    this.vesselVisitService.getById(this.notificationId).subscribe({
      next: (notification) => {
        this.populateForm(notification);
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading notification:', err);
        this.error = 'Failed to load notification for editing.';
        this.loading = false;
      }
    });
  }

  private populateForm(notification: VesselVisitNotificationDTO): void {
    this.form.patchValue({
      vesselImoNumber: notification.vesselImoNumber,
      estimatedTimeOfArrival: this.formatDateForInput(notification.estimatedTimeOfArrival),
      estimatedTimeOfDeparture: this.formatDateForInput(notification.estimatedTimeOfDeparture),
      captainName: notification.captainName || '',
      crewCount: notification.crewCount || null,
      preferredDockCode: notification.preferredDockCode || ''
    });

    // Populate containers
    if (notification.containerIds && notification.containerIds.length > 0) {
      notification.containerIds.forEach(id => this.addContainer(id));
    }

    // Populate crew
    if (notification.crew && notification.crew.length > 0) {
      notification.crew.forEach(member => this.addCrewMember(member));
    }

    // Populate safety officers
    if (notification.safetyOfficers && notification.safetyOfficers.length > 0) {
      notification.safetyOfficers.forEach(officer => this.addSafetyOfficer(officer));
    }
  }

  private formatDateForInput(dateString?: string): string {
    if (!dateString) return '';
    // Convert to local datetime-local format: YYYY-MM-DDTHH:mm
    const date = new Date(dateString);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  // Container management
  get containers(): FormArray {
    return this.form.get('containerIds') as FormArray;
  }

  addContainer(value: string = ''): void {
    this.containers.push(this.fb.control(value, [Validators.required, Validators.pattern(/^[A-Z]{4}\d{7}$/), this.containerCheckDigitValidator.bind(this)]));
  }

  private containerCheckDigitValidator(control: any): { [key: string]: any } | null {
    const value = control.value;
    if (!value || value.length !== 11) {
      return null; // Let the pattern validator handle this
    }

    // ISO 6346 check digit validation
    const code = value.toUpperCase();
    
    // Exact ISO 6346 letter mapping table
    const letterValues: { [key: string]: number } = {
      'A': 10, 'B': 12, 'C': 13, 'D': 14, 'E': 15, 'F': 16, 'G': 17, 'H': 18, 'I': 19, 'J': 20,
      'K': 21, 'L': 23, 'M': 24, 'N': 25, 'O': 26, 'P': 27, 'Q': 28, 'R': 29, 'S': 30, 'T': 31,
      'U': 32, 'V': 34, 'W': 35, 'X': 36, 'Y': 37, 'Z': 38
    };

    let sum = 0;
    // Process first 10 characters (4 letters + 6 digits)
    for (let i = 0; i < 10; i++) {
      const char = code[i];
      const charValue = char >= 'A' && char <= 'Z' ? letterValues[char] : parseInt(char, 10);
      sum += charValue * Math.pow(2, i);
    }

    const calculatedCheckDigit = (sum % 11) % 10;
    const providedCheckDigit = parseInt(code[10], 10);

    if (calculatedCheckDigit !== providedCheckDigit) {
      return { 'invalidContainerCheckDigit': true };
    }

    return null;
  }

  removeContainer(index: number): void {
    this.containers.removeAt(index);
  }

  // Crew management
  get crewMembers(): FormArray {
    return this.form.get('crew') as FormArray;
  }

  addCrewMember(member?: CrewMemberDTO): void {
    this.crewMembers.push(this.fb.group({
      name: [member?.name || '', Validators.required],
      citizenId: [member?.citizenId || '', Validators.required],
      nationality: [member?.nationality || '', Validators.required]
    }));
  }

  removeCrewMember(index: number): void {
    this.crewMembers.removeAt(index);
  }

  // Safety officers management
  get safetyOfficersList(): FormArray {
    return this.form.get('safetyOfficers') as FormArray;
  }

  addSafetyOfficer(name: string = ''): void {
    this.safetyOfficersList.push(this.fb.control(name, Validators.required));
  }

  removeSafetyOfficer(index: number): void {
    this.safetyOfficersList.removeAt(index);
  }

  /**
   * Save as draft (InProgress status)
   * US 2.2.8 - AC: Vessel Visit Notifications might become at an "in progress" status
   */
  saveAsDraft(): void {
    if (!this.form.get('vesselImoNumber')?.valid) {
      this.error = 'At minimum, Vessel IMO number is required to save as draft.';
      return;
    }

    this.submitForm(false);
  }

  /**
   * Submit for approval (Submitted status)
   * US 2.2.8 - AC: When completed / ready for asking approval, the agent is required to change its state to "submitted"
   */
  submitForApproval(): void {
    if (!this.form.valid) {
      if (this.form.errors?.['invalidDateRange']) {
        this.error = 'Estimated Time of Departure must be after Estimated Time of Arrival.';
      } else {
        this.error = 'Please fill in all required fields before submitting.';
      }
      this.markFormGroupTouched(this.form);
      return;
    }

    this.submitForm(true);
  }

  private submitForm(submitForApproval: boolean): void {
    this.loading = true;
    this.error = null;
    this.successMessage = null;

    const formValue = this.form.value;
    
    // Always create as InProgress first, then submit if needed
    const dto: Partial<VesselVisitNotificationDTO> = {
      vesselImoNumber: formValue.vesselImoNumber,
      estimatedTimeOfArrival: formValue.estimatedTimeOfArrival,
      estimatedTimeOfDeparture: formValue.estimatedTimeOfDeparture,
      captainName: formValue.captainName || undefined,
      crewCount: formValue.crewCount || undefined,
      preferredDockCode: formValue.preferredDockCode || undefined,
      containerIds: formValue.containerIds || [],
      crew: formValue.crew || [],
      safetyOfficers: formValue.safetyOfficers || []
      // organizationIdentifier will be set automatically by backend from logged user
    };

    const saveOperation = this.isEditMode && this.notificationId
      ? this.vesselVisitService.update(this.notificationId, dto as any)
      : this.vesselVisitService.create(dto as any);

    saveOperation.subscribe({
      next: (result: any) => {
        if (!result.id) {
          this.error = 'Failed to create notification - no ID returned';
          this.loading = false;
          return;
        }

        // If submit for approval, call submit endpoint
        if (submitForApproval) {
          this.vesselVisitService.submit(result.id).subscribe({
            next: () => {
              this.successMessage = 'Vessel Visit Notification submitted successfully!';
              this.loading = false;
              setTimeout(() => this.router.navigate(['/vessel-visits']), 2000);
            },
            error: (err) => {
              console.error('Error submitting notification:', err);
              if (Array.isArray(err.error)) {
                this.error = err.error.join(', ');
              } else if (err.error?.message) {
                this.error = err.error.message;
              } else if (typeof err.error === 'string') {
                this.error = err.error;
              } else {
                this.error = 'Failed to submit notification. Please check all fields.';
              }
              this.loading = false;
            }
          });
        } else {
          const action = this.isEditMode ? 'updated' : 'created';
          this.successMessage = `Vessel Visit Notification ${action} as draft successfully!`;
          this.loading = false;
          setTimeout(() => this.router.navigate(['/vessel-visits']), 2000);
        }
      },
      error: (err: any) => {
        console.error('Error saving notification:', err);
        if (Array.isArray(err.error)) {
          this.error = err.error.join(', ');
        } else if (err.error?.message) {
          this.error = err.error.message;
        } else if (typeof err.error === 'string') {
          this.error = err.error;
        } else {
          this.error = 'Failed to save notification. Please check all fields.';
        }
        this.loading = false;
      }
    });
  }

  private markFormGroupTouched(formGroup: FormGroup | FormArray): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup || control instanceof FormArray) {
        this.markFormGroupTouched(control);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/vessel-visits']);
  }

  // Helper method to check if field has error
  hasError(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  // Helper method to get error message
  getErrorMessage(fieldName: string): string {
    const field = this.form.get(fieldName);
    if (!field || !field.errors) return '';

    if (field.errors['required']) return 'This field is required';
    if (field.errors['invalidImoCheckDigit']) return 'Invalid IMO check digit. Please verify the IMO number.';
    if (field.errors['invalidContainerCheckDigit']) return 'Invalid container check digit. Please verify the container ID.';
    if (field.errors['pattern']) {
      if (fieldName === 'vesselImoNumber') return 'IMO number must be 7 digits';
      if (fieldName.includes('container')) return 'Container ID must be 4 letters + 7 digits (e.g., MSCU1234567)';
    }
    if (field.errors['min']) return `Minimum value is ${field.errors['min'].min}`;

    return 'Invalid value';
  }

  // Helper to check if form has date range error
  hasDateRangeError(): boolean {
    return !!(this.form.errors?.['invalidDateRange'] && 
             (this.form.get('estimatedTimeOfArrival')?.touched || 
              this.form.get('estimatedTimeOfDeparture')?.touched));
  }
}
