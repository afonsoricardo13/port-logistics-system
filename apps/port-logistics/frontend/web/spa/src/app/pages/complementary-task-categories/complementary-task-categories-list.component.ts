import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ComplementaryTaskCategoryService, ComplementaryTaskCategoryDTO } from '../../services/complementary-task-category.service';

@Component({
  selector: 'app-complementary-task-categories-list',
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule],
  templateUrl: './complementary-task-categories-list.component.html',
  styleUrl: './complementary-task-categories-list.component.scss'
})
export class ComplementaryTaskCategoriesListComponent implements OnInit {
  categories: ComplementaryTaskCategoryDTO[] = [];
  filteredCategories: ComplementaryTaskCategoryDTO[] = [];
  loading = false;
  error: string | null = null;
  searchTerm = '';
  showDeleteModal = false;
  deleteTarget: { code: string; name: string } | null = null;

  constructor(private categoryService: ComplementaryTaskCategoryService) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading = true;
    this.error = null;
    
    this.categoryService.getAll().subscribe({
      next: (data) => {
        this.categories = data;
        this.applyFilter();
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to load complementary task categories';
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    const term = this.searchTerm.toLowerCase().trim();
    if (!term) {
      this.filteredCategories = [...this.categories];
    } else {
      this.filteredCategories = this.categories.filter(c =>
        c.code.toLowerCase().includes(term) ||
        c.name.toLowerCase().includes(term) ||
        c.description.toLowerCase().includes(term)
      );
    }
  }

  onSearchChange(): void {
    this.applyFilter();
  }

  deleteCategory(code: string, name: string): void {
    this.deleteTarget = { code, name };
    this.showDeleteModal = true;
  }

  confirmDelete(): void {
    if (!this.deleteTarget) return;

    this.categoryService.delete(this.deleteTarget.code).subscribe({
      next: () => {
        this.showDeleteModal = false;
        this.deleteTarget = null;
        this.loadCategories();
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to delete category';
        this.showDeleteModal = false;
        this.deleteTarget = null;
      }
    });
  }

  cancelDelete(): void {
    this.showDeleteModal = false;
    this.deleteTarget = null;
  }

  formatDuration(minutes?: number | null): string {
    if (!minutes) return 'N/A';
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours > 0 && mins > 0) return `${hours}h ${mins}m`;
    if (hours > 0) return `${hours}h`;
    return `${mins}m`;
  }
}
