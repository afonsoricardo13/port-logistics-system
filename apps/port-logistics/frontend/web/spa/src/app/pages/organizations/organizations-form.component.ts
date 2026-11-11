// US 2.2.5: Register Shipping Agent Organizations with initial representative
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { OrganizationService, CreateOrganizationDTO, OrganizationDTO } from '../../services/organization.service';

@Component({
  selector: 'app-organizations-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './organizations-form.component.html',
  styleUrls: ['./organizations-form.component.scss']
})
export class OrganizationsFormComponent implements OnInit {
  isEditMode = false;
  organizationId: number | null = null;
  loading = false;
  error: string | null = null;
  
  // Organization fields
  identifier = '';
  legalName = '';
  alternativeName = '';
  taxNumber = '';
  street = '';
  postalCode = '';
  city = '';

  // Initial representative fields (required for US 2.2.5 - only in create mode)
  representativeName = '';
  representativeEmail = '';
  representativeCitizenId = '';
  representativeNationality = '';
  representativePhoneNumber = '';

  constructor(
    private organizationService: OrganizationService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.organizationId = +id;
      this.loadOrganization();
    }
  }

  loadOrganization(): void {
    if (!this.organizationId) return;

    this.loading = true;
    this.error = null;

    this.organizationService.getById(this.organizationId).subscribe({
      next: (org) => {
        this.identifier = org.identifier;
        this.legalName = org.legalName;
        this.alternativeName = org.alternativeName;
        this.taxNumber = org.taxNumber;
        this.street = org.address.street;
        this.postalCode = org.address.postalCode;
        this.city = org.address.city;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading organization:', err);
        this.error = 'Failed to load organization';
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.isEditMode) {
      this.updateOrganization();
    } else {
      this.createOrganization();
    }
  }

  createOrganization(): void {
    // Validate organization fields
    if (!this.identifier || !this.legalName || !this.taxNumber || !this.street || !this.postalCode || !this.city) {
      this.error = 'Please fill in all organization fields';
      return;
    }

    // Validate representative fields (required for US 2.2.5)
    if (!this.representativeName || !this.representativeEmail || !this.representativeCitizenId || 
        !this.representativeNationality || !this.representativePhoneNumber) {
      this.error = 'Please fill in all representative fields. Each organization must include at least one representative.';
      return;
    }

    this.loading = true;
    this.error = null;

    const dto: CreateOrganizationDTO = {
      identifier: this.identifier,
      legalName: this.legalName,
      alternativeName: this.alternativeName,
      taxNumber: this.taxNumber,
      address: {
        street: this.street,
        postalCode: this.postalCode,
        city: this.city
      },
      initialRepresentative: {
        name: this.representativeName,
        email: this.representativeEmail,
        citizenId: this.representativeCitizenId,
        nationality: this.representativeNationality,
        phoneNumber: this.representativePhoneNumber
      }
    };

    this.organizationService.create(dto).subscribe({
      next: () => {
        this.router.navigate(['/organizations']);
      },
      error: (err) => {
        console.error('Error creating organization:', err);
        this.error = err.error?.message || 'Failed to create organization';
        this.loading = false;
      }
    });
  }

  updateOrganization(): void {
    if (!this.organizationId) return;

    // Validate organization fields
    if (!this.legalName || !this.taxNumber || !this.street || !this.postalCode || !this.city) {
      this.error = 'Please fill in all fields';
      return;
    }

    this.loading = true;
    this.error = null;

    const dto = {
      legalName: this.legalName,
      alternativeName: this.alternativeName,
      taxNumber: this.taxNumber,
      address: {
        street: this.street,
        postalCode: this.postalCode,
        city: this.city
      }
    };

    this.organizationService.update(this.organizationId, dto).subscribe({
      next: () => {
        this.router.navigate(['/organizations']);
      },
      error: (err) => {
        console.error('Error updating organization:', err);
        this.error = err.error?.message || 'Failed to update organization';
        this.loading = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/organizations']);
  }
}
