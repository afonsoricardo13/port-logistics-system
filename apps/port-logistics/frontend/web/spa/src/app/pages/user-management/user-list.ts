import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { UserManagementService, SystemUser } from '../../services/user-management.service';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-list.html',
  styleUrls: ['./user-list.scss']
})
export class UserListComponent implements OnInit {
  users: SystemUser[] = [];
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;

  constructor(
    private userManagementService: UserManagementService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.error = null;

    this.userManagementService.getAllUsers().subscribe({
      next: (users) => {
        this.users = users;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading users:', err);
        this.error = 'Failed to load users. Please try again.';
        this.loading = false;
      }
    });
  }

  createNewUser(): void {
    this.router.navigate(['/user-management/create']);
  }

  editUser(email: string): void {
    this.router.navigate(['/user-management/edit', email]);
  }

  resendActivation(email: string): void {
    if (!confirm(`Resend activation email to ${email}?`)) {
      return;
    }

    this.userManagementService.resendActivationEmail(email).subscribe({
      next: () => {
        this.successMessage = `Activation email sent successfully to ${email}`;
        setTimeout(() => this.successMessage = null, 5000);
      },
      error: (err) => {
        console.error('Error resending activation email:', err);
        this.error = 'Failed to resend activation email.';
      }
    });
  }

  getStatusClass(user: SystemUser): string {
    if (user.active) {
      return 'status-active';
    } else if (user.hasPendingActivation) {
      return 'status-pending';
    } else {
      return 'status-inactive';
    }
  }

  getStatusText(user: SystemUser): string {
    if (user.active) {
      return 'Active';
    } else if (user.hasPendingActivation) {
      return 'Pending Activation';
    } else {
      return 'Inactive';
    }
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString();
  }
}
