import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { QualificationService, QualificationDTO } from '../../services/qualification.service';

/**
 * Qualifications List Component
 * 
 * US 2.2.13: Display all qualifications with search/filter capabilities
 * Allows logistics operators to view and manage qualifications
 */
@Component({
  selector: 'app-qualifications-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './qualifications-list.component.html',
  styleUrls: ['./qualifications-list.component.scss']
})
export class QualificationsListComponent implements OnInit {
  qualifications: QualificationDTO[] = [];
  filteredQualifications: QualificationDTO[] = [];
  searchTerm: string = '';
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(
    private qualificationService: QualificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadQualifications();
  }

  /**
   * Load all qualifications from the backend
   */
  loadQualifications(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.qualificationService.getAll().subscribe({
      next: (data) => {
        this.qualifications = data;
        this.filteredQualifications = data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading qualifications:', error);
        this.errorMessage = 'Failed to load qualifications. Please try again.';
        this.isLoading = false;
      }
    });
  }

  /**
   * Filter qualifications based on search term
   * Searches in qualificationName field
   */
  filterQualifications(): void {
    if (!this.searchTerm.trim()) {
      this.filteredQualifications = this.qualifications;
      return;
    }

    const term = this.searchTerm.toLowerCase();
    this.filteredQualifications = this.qualifications.filter(q =>
      q.qualificationName.toLowerCase().includes(term)
    );
  }

  /**
   * Navigate to create new qualification form
   */
  createQualification(): void {
    this.router.navigate(['/qualifications/new']);
  }

  /**
   * Navigate to edit qualification form
   */
  editQualification(qualificationName: string): void {
    this.router.navigate(['/qualifications', qualificationName, 'edit']);
  }

  /**
   * Clear search filter
   */
  clearSearch(): void {
    this.searchTerm = '';
    this.filterQualifications();
  }
}
