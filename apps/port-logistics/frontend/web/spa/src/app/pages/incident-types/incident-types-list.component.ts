import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { IncidentTypeService, IncidentTypeDTO } from '../../services/incident-type.service';

@Component({
  selector: 'app-incident-types-list',
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule],
  templateUrl: './incident-types-list.component.html',
  styleUrl: './incident-types-list.component.scss'
})
export class IncidentTypesListComponent implements OnInit {
  incidentTypes: IncidentTypeDTO[] = [];
  filteredTypes: IncidentTypeDTO[] = [];
  loading = false;
  error: string | null = null;
  filterRootOnly = false;
  filterSeverity: string = 'all';
  filterParent: string = 'all';
  parentTypes: IncidentTypeDTO[] = [];
  showDeleteModal = false;
  deleteTarget: { code: string; name: string } | null = null;

  constructor(private incidentTypeService: IncidentTypeService) {}

  ngOnInit(): void {
    this.loadIncidentTypes();
  }

  loadIncidentTypes(): void {
    this.loading = true;
    this.error = null;
    
    this.incidentTypeService.getAll().subscribe({
      next: (data) => {
        this.incidentTypes = data;
        this.parentTypes = data; // Show ALL types as potential parents for filtering
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to load incident types';
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.incidentTypes];

    if (this.filterRootOnly) {
      filtered = filtered.filter(t => !t.parentTypeCode);
    }

    if (this.filterSeverity !== 'all') {
      filtered = filtered.filter(t => t.severity === this.filterSeverity);
    }

    if (this.filterParent !== 'all') {
      filtered = filtered.filter(t => t.parentTypeCode === this.filterParent);
    }

    this.filteredTypes = filtered;
  }

  toggleRootFilter(): void {
    this.filterRootOnly = !this.filterRootOnly;
    this.filterParent = 'all';
    this.applyFilters();
  }

  onSeverityChange(): void {
    this.applyFilters();
  }

  onParentChange(): void {
    this.filterRootOnly = false;
    this.applyFilters();
  }

  deleteIncidentType(code: string, name: string): void {
    this.deleteTarget = { code, name };
    this.showDeleteModal = true;
  }

  confirmDelete(): void {
    if (!this.deleteTarget) return;

    this.incidentTypeService.delete(this.deleteTarget.code).subscribe({
      next: () => {
        this.showDeleteModal = false;
        this.deleteTarget = null;
        this.loadIncidentTypes();
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to delete incident type';
        this.showDeleteModal = false;
        this.deleteTarget = null;
      }
    });
  }

  cancelDelete(): void {
    this.showDeleteModal = false;
    this.deleteTarget = null;
  }

  getSeverityClass(severity: string): string {
    return `severity-${severity.toLowerCase()}`;
  }
}
