import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { IncidentTypeService, CreateIncidentTypeDTO, UpdateIncidentTypeDTO, IncidentTypeDTO } from '../../services/incident-type.service';

@Component({
  selector: 'app-incident-types-form',
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './incident-types-form.component.html',
  styleUrl: './incident-types-form.component.scss'
})
export class IncidentTypesFormComponent implements OnInit {
  isEditMode = false;
  code = '';
  name = '';
  description = '';
  severity = 'Minor';
  parentTypeCode: string | null = null;
  
  availableParents: IncidentTypeDTO[] = [];
  loading = false;
  error: string | null = null;
  
  severityOptions = ['Minor', 'Major', 'Critical'];

  constructor(
    private incidentTypeService: IncidentTypeService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const code = this.route.snapshot.paramMap.get('code');
    
    if (code) {
      this.isEditMode = true;
      this.code = code;
      this.loadIncidentType(code);
    }
    
    this.loadAvailableParents();
  }

  loadIncidentType(code: string): void {
    this.loading = true;
    this.incidentTypeService.getByCode(code).subscribe({
      next: (data) => {
        this.name = data.name;
        this.description = data.description;
        this.severity = data.severity;
        this.parentTypeCode = data.parentTypeCode || null;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to load incident type';
        this.loading = false;
      }
    });
  }

  loadAvailableParents(): void {
    this.incidentTypeService.getAll().subscribe({
      next: (data) => {
        this.availableParents = data.filter(t => !this.isEditMode || t.code !== this.code);
      },
      error: () => {
        // Silently fail - parent selection is optional
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
      const dto: UpdateIncidentTypeDTO = {
        name: this.name,
        description: this.description,
        severity: this.severity,
        parentTypeCode: this.parentTypeCode
      };

      this.incidentTypeService.update(this.code, dto).subscribe({
        next: () => {
          this.router.navigate(['/operations/incident-types']);
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to update incident type';
          this.loading = false;
        }
      });
    } else {
      const dto: CreateIncidentTypeDTO = {
        code: this.code,
        name: this.name,
        description: this.description,
        severity: this.severity,
        parentTypeCode: this.parentTypeCode
      };

      this.incidentTypeService.create(dto).subscribe({
        next: () => {
          this.router.navigate(['/operations/incident-types']);
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to create incident type';
          this.loading = false;
        }
      });
    }
  }

  isValid(): boolean {
    if (!this.isEditMode && !this.code.trim()) {
      this.error = 'Code is required';
      return false;
    }
    if (!this.name.trim()) {
      this.error = 'Name is required';
      return false;
    }
    if (!this.description.trim()) {
      this.error = 'Description is required';
      return false;
    }
    return true;
  }

  cancel(): void {
    this.router.navigate(['/operations/incident-types']);
  }
}
