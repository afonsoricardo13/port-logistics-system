import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { IncidentsService, CreateIncidentDTO, UpdateIncidentDTO, IncidentDTO } from '../../services/incidents.service';
import { IncidentTypeService, IncidentTypeDTO } from '../../services/incident-type.service';
import { AuthService } from '../../services/auth.service';

/**
 * US 4.1.13 - Create/Edit Incident Form
 */
@Component({
  selector: 'app-incidents-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './incidents-form.component.html',
  styleUrls: ['./incidents-form.component.scss']
})
export class IncidentsFormComponent implements OnInit {
  isEditMode = false;
  incidentId = '';
  
  // Form fields
  incidentTypeCode = '';
  startTime = '';
  endTime = '';
  severity = 'Minor';
  description = '';
  status = 'Active';
  affectedVVEIds: string = ''; // Comma-separated input
  
  // Options
  incidentTypes: IncidentTypeDTO[] = [];
  severityOptions = ['Minor', 'Major', 'Critical'];
  statusOptions = ['Active', 'Resolved'];
  
  // State
  loading = false;
  error: string | null = null;
  currentIncident: IncidentDTO | null = null;

  constructor(
    private incidentsService: IncidentsService,
    private incidentTypeService: IncidentTypeService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    
    this.loadIncidentTypes();
    
    if (id) {
      this.isEditMode = true;
      this.incidentId = id;
      this.loadIncident(id);
    } else {
      // Set default start time to now
      const now = new Date();
      this.startTime = this.formatDateTimeLocal(now);
    }
  }

  loadIncidentTypes(): void {
    this.incidentTypeService.getAll().subscribe({
      next: (data) => {
        this.incidentTypes = data;
        // Select first if available and not edit mode
        if (!this.isEditMode && data.length > 0) {
          this.incidentTypeCode = data[0].code;
        }
      },
      error: () => {
        this.error = 'Failed to load incident types';
      }
    });
  }

  loadIncident(id: string): void {
    this.loading = true;
    this.incidentsService.getById(id).subscribe({
      next: (data) => {
        this.currentIncident = data;
        this.incidentTypeCode = data.incidentTypeCode;
        this.startTime = this.formatDateTimeLocal(new Date(data.startTime));
        if (data.endTime) {
          this.endTime = this.formatDateTimeLocal(new Date(data.endTime));
        }
        this.severity = data.severity;
        this.description = data.description;
        this.status = data.status;
        
        // Format affected VVEs for input
        if (data.affectedVVEIds.length > 0) {
          this.affectedVVEIds = data.affectedVVEIds.join(', ');
        }
        
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to load incident';
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    if (!this.isValid()) {
      return;
    }

    this.loading = true;
    this.error = null;

    if (this.isEditMode) {
      this.updateIncident();
    } else {
      this.createIncident();
    }
  }

  createIncident(): void {
    const userEmail = this.authService.getUserEmail() || 'system@portlogistics.com';
    
    const dto: CreateIncidentDTO = {
      incidentTypeCode: this.incidentTypeCode,
      startTime: new Date(this.startTime).toISOString(),
      severity: this.severity,
      description: this.description,
      createdBy: userEmail,
      affectedVVEIds: this.parseAffectedVVEIds()
    };

    this.incidentsService.create(dto).subscribe({
      next: () => {
        this.router.navigate(['/operations/incidents']);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to create incident';
        this.loading = false;
      }
    });
  }

  updateIncident(): void {
    const dto: UpdateIncidentDTO = {
      incidentTypeCode: this.incidentTypeCode,
      severity: this.severity,
      description: this.description,
      status: this.status,
      affectedVVEIds: this.parseAffectedVVEIds()
    };

    // Include end time if provided
    if (this.endTime) {
      dto.endTime = new Date(this.endTime).toISOString();
    }

    this.incidentsService.update(this.incidentId, dto).subscribe({
      next: () => {
        this.router.navigate(['/operations/incidents']);
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to update incident';
        this.loading = false;
      }
    });
  }

  parseAffectedVVEIds(): string[] {
    if (!this.affectedVVEIds.trim()) {
      return []; // Empty means all ongoing VVEs
    }
    
    return this.affectedVVEIds
      .split(',')
      .map(id => id.trim())
      .filter(id => id.length > 0);
  }

  isValid(): boolean {
    if (!this.incidentTypeCode) {
      this.error = 'Please select an incident type';
      return false;
    }
    if (!this.startTime) {
      this.error = 'Start time is required';
      return false;
    }
    if (!this.description.trim()) {
      this.error = 'Description is required';
      return false;
    }
    
    // Validate end time is after start time if both provided
    if (this.endTime && new Date(this.endTime) <= new Date(this.startTime)) {
      this.error = 'End time must be after start time';
      return false;
    }
    
    return true;
  }

  cancel(): void {
    this.router.navigate(['/operations/incidents']);
  }

  /**
   * Format date for datetime-local input
   */
  private formatDateTimeLocal(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }
}
