import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { UserManagementService, CreateUserRequest, UpdateUserRequest, SystemUser } from '../../services/user-management.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-form.html',
  styleUrls: ['./user-form.scss']
})
export class UserFormComponent implements OnInit {
  isEditMode = false;
  loading = false;
  error: string | null = null;
  
  email = '';
  name = '';
  role = '';
  active = false;
  
  availableRoles: string[] = [];
  originalEmail = ''; // For edit mode

  constructor(
    private userManagementService: UserManagementService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.availableRoles = this.userManagementService.getAvailableRoles();
    
    // Check if we're in edit mode
    const emailParam = this.route.snapshot.paramMap.get('email');
    if (emailParam) {
      this.isEditMode = true;
      this.originalEmail = emailParam;
      this.loadUser(emailParam);
    }
  }

  loadUser(email: string): void {
    this.loading = true;
    this.userManagementService.getUserByEmail(email).subscribe({
      next: (user: SystemUser) => {
        this.email = user.email;
        this.name = user.name;
        this.role = user.role;
        this.active = user.active;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading user:', err);
        this.error = 'Failed to load user data';
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    // Validation
    if (!this.email || !this.name || !this.role) {
      this.error = 'Please fill in all required fields';
      return;
    }

    if (!this.isValidEmail(this.email)) {
      this.error = 'Please enter a valid email address';
      return;
    }

    this.loading = true;
    this.error = null;

    if (this.isEditMode) {
      this.updateUser();
    } else {
      this.createUser();
    }
  }

  createUser(): void {
    const request: CreateUserRequest = {
      email: this.email.trim(),
      name: this.name.trim(),
      role: this.role
    };

    const userProfile = this.authService.getUserProfile();
    const currentUserEmail = userProfile?.email || 'system';

    this.userManagementService.createUser(request, currentUserEmail).subscribe({
      next: () => {
        alert(`User created successfully! Activation email sent to ${this.email}`);
        this.router.navigate(['/user-management']);
      },
      error: (err) => {
        console.error('Error creating user:', err);
        this.error = err.error?.message || 'Failed to create user. Please try again.';
        this.loading = false;
      }
    });
  }

  updateUser(): void {
    const request: UpdateUserRequest = {
      email: this.originalEmail,
      name: this.name.trim(),
      role: this.role,
      active: this.active
    };

    this.userManagementService.updateUser(request).subscribe({
      next: () => {
        alert('User updated successfully!');
        this.router.navigate(['/user-management']);
      },
      error: (err) => {
        console.error('Error updating user:', err);
        this.error = err.error?.message || 'Failed to update user. Please try again.';
        this.loading = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/user-management']);
  }

  isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }
}
