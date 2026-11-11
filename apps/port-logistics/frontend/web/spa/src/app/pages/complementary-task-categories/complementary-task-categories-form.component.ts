import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ComplementaryTaskCategoryService, CreateComplementaryTaskCategoryDTO, UpdateComplementaryTaskCategoryDTO } from '../../services/complementary-task-category.service';

@Component({
  selector: 'app-complementary-task-categories-form',
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './complementary-task-categories-form.component.html',
  styleUrl: './complementary-task-categories-form.component.scss'
})
export class ComplementaryTaskCategoriesFormComponent implements OnInit {
  isEditMode = false;
  code = '';
  name = '';
  description = '';
  defaultDuration: number | null = null;
  expectedImpact = '';
  
  loading = false;
  error: string | null = null;
  success: string | null = null;

  constructor(
    private categoryService: ComplementaryTaskCategoryService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    const code = this.route.snapshot.paramMap.get('code');
    
    if (code) {
      this.isEditMode = true;
      this.code = code;
      this.loadCategory(code);
    }
  }

  loadCategory(code: string): void {
    this.loading = true;
    this.categoryService.getByCode(code).subscribe({
      next: (data) => {
        this.name = data.name;
        this.description = data.description;
        this.defaultDuration = data.defaultDuration || null;
        this.expectedImpact = data.expectedImpact || '';
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to load category';
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
      const dto: UpdateComplementaryTaskCategoryDTO = {
        name: this.name,
        description: this.description,
        defaultDuration: this.defaultDuration,
        expectedImpact: this.expectedImpact || null
      };

      this.categoryService.update(this.code, dto).subscribe({
        next: () => {
          this.success = 'Category updated successfully!';
          setTimeout(() => {
            this.router.navigate(['/operations/task-categories']);
          }, 1500);
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to update category';
          this.loading = false;
        }
      });
    } else {
      const dto: CreateComplementaryTaskCategoryDTO = {
        code: this.code,
        name: this.name,
        description: this.description,
        defaultDuration: this.defaultDuration,
        expectedImpact: this.expectedImpact || null
      };

      this.categoryService.create(dto).subscribe({
        next: () => {
          this.success = 'Category created successfully!';
          setTimeout(() => {
            this.router.navigate(['/operations/task-categories']);
          }, 1500);
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to create category';
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
    if (this.defaultDuration !== null && this.defaultDuration < 0) {
      this.error = 'Default duration must be positive';
      return false;
    }
    return true;
  }

  cancel(): void {
    this.router.navigate(['/operations/task-categories']);
  }
}
