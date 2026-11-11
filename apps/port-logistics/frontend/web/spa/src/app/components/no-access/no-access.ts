import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

/**
 * No Access Component
 * US 3.2.4: Display access denied message when user lacks required permission
 */
@Component({
  selector: 'app-no-access',
  imports: [CommonModule],
  templateUrl: './no-access.html',
  styleUrl: './no-access.scss',
})
export class NoAccess implements OnInit {
  requiredPermission: string | null = null;
  requiredPermissions: string[] = [];
  attemptedUrl: string | null = null;
  userRole: string | null = null;
  userPermissions: string[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Get query parameters from route
    this.route.queryParams.subscribe(params => {
      this.requiredPermission = params['requiredPermission'] || null;
      this.requiredPermissions = params['requiredPermissions']?.split(',') || [];
      this.attemptedUrl = params['attemptedUrl'] || null;
    });

    // Get user's current role and permissions
    this.userRole = this.authService.getUserRole();
    this.userPermissions = this.authService.getUserPermissions();
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}
