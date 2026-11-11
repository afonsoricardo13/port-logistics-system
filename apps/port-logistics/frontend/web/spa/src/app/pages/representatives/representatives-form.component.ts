// US 2.2.6: Register and Manage Representatives (create, update, deactivate)
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { RepresentativeService, RepresentativeDTO } from '../../services/representative.service';
import { OrganizationService, OrganizationDTO } from '../../services/organization.service';

@Component({
  selector: 'app-representatives-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './representatives-form.component.html',
  styleUrls: ['./representatives-form.component.scss']
})
export class RepresentativesFormComponent implements OnInit {
  isEditMode = false;
  representativeId: number | null = null;
  loading = false;
  error: string | null = null;
  
  organizations: OrganizationDTO[] = [];
  
  // Representative fields
  name = '';
  email = '';
  citizenId = '';
  nationality = '';
  phoneNumber = '';
  organizationId: number | null = null;

  constructor(
    private representativeService: RepresentativeService,
    private organizationService: OrganizationService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.loadOrganizations();
    
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.representativeId = +id;
      this.loadRepresentative();
    } else {
      // Check if pre-selecting organization from query params
      this.route.queryParams.subscribe(params => {
        if (params['organizationId']) {
          this.organizationId = +params['organizationId'];
        }
      });
    }
  }

  loadOrganizations(): void {
    this.organizationService.getAll().subscribe({
      next: (data) => {
        this.organizations = data;
      },
      error: (err) => {
        console.error('Error loading organizations:', err);
        this.error = 'Failed to load organizations';
      }
    });
  }

  loadRepresentative(): void {
    if (!this.representativeId) return;

    this.loading = true;
    this.error = null;

    this.representativeService.getById(this.representativeId).subscribe({
      next: (rep) => {
        this.name = rep.name;
        this.email = rep.email;
        this.citizenId = rep.citizenId || '';
        this.nationality = rep.nationality || '';
        this.phoneNumber = rep.phoneNumber || '';
        this.organizationId = rep.organizationId;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading representative:', err);
        this.error = 'Failed to load representative';
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.isEditMode) {
      this.updateRepresentative();
    } else {
      this.createRepresentative();
    }
  }

  createRepresentative(): void {
    // Validate required fields
    if (!this.name || !this.email || !this.citizenId || !this.nationality || !this.phoneNumber || !this.organizationId) {
      this.error = 'Please fill in all required fields';
      return;
    }

    this.loading = true;
    this.error = null;

    const dto = {
      name: this.name,
      email: this.email,
      citizenId: this.citizenId,
      nationality: this.nationality,
      phoneNumber: this.phoneNumber,
      organizationId: this.organizationId
    };

    this.representativeService.create(dto).subscribe({
      next: () => {
        this.router.navigate(['/representatives']);
      },
      error: (err) => {
        console.error('Error creating representative:', err);
        this.error = err.error?.message || 'Failed to create representative';
        this.loading = false;
      }
    });
  }

  updateRepresentative(): void {
    if (!this.representativeId) return;

    // Validate required editable fields
    if (!this.name || !this.email || !this.nationality || !this.phoneNumber) {
      this.error = 'Please fill in all required fields';
      return;
    }

    this.loading = true;
    this.error = null;

    // Only send editable fields according to UpdateRepresentativeDTO
    const dto = {
      name: this.name,
      email: this.email,
      nationality: this.nationality,
      phoneNumber: this.phoneNumber
    };

    this.representativeService.update(this.representativeId, dto).subscribe({
      next: () => {
        this.router.navigate(['/representatives']);
      },
      error: (err) => {
        console.error('Error updating representative:', err);
        this.error = err.error?.message || 'Failed to update representative';
        this.loading = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/representatives']);
  }
}
