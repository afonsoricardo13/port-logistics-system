import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-navbar',
  imports: [CommonModule, RouterLink, RouterLinkActive, TranslateModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar implements OnInit {
  currentLang = 'en';
  userRole = '';
  userName = 'Admin User';
  userEmail = '';
  userPicture = '';

  // Menu items baseados em role (dynamic navigation - AC)
  menuItems = [
    { route: '/dashboard', label: 'nav.dashboard', roles: ['Administrator', 'LogisticsOperator', 'PortAuthorityOfficer', 'PortOperationsSupervisor', 'ShippingAgentRepresentative'] },
    { route: '/user-management', label: 'nav.userManagement', roles: ['Administrator'] },
    { route: '/admin/privacy-policy', label: 'nav.privacyPolicyAdmin', roles: ['Administrator'] },
    { route: '/admin/data-rights', label: 'nav.dataRightsAdmin', roles: ['Administrator'] },
    { route: '/privacy-policy', label: 'nav.privacyPolicy', roles: ['LogisticsOperator', 'PortAuthorityOfficer', 'PortOperationsSupervisor', 'ShippingAgentRepresentative'] },
    { route: '/data-rights', label: 'nav.dataRights', roles: ['Administrator', 'LogisticsOperator', 'PortAuthorityOfficer', 'PortOperationsSupervisor', 'ShippingAgentRepresentative'] },
    { route: '/vessel-visits', label: 'nav.vesselVisits', roles: ['Administrator', 'LogisticsOperator', 'PortAuthorityOfficer', 'ShippingAgentRepresentative'] },
    { route: '/operating-staff', label: 'nav.operatingStaff', roles: ['Administrator', 'LogisticsOperator'] },
    { route: '/docks', label: 'nav.docks', roles: ['Administrator', 'PortAuthorityOfficer'] },
    { route: '/storage-areas', label: 'nav.storageAreas', roles: ['Administrator', 'PortAuthorityOfficer'] },
    { route: '/vessels', label: 'nav.vessels', roles: ['Administrator', 'PortAuthorityOfficer', 'ShippingAgentRepresentative'] },
    { route: '/vessel-types', label: 'nav.vesselTypes', roles: ['Administrator', 'PortAuthorityOfficer'] },
    { route: '/organizations', label: 'nav.organizations', roles: ['Administrator', 'PortAuthorityOfficer'] },
    { route: '/representatives', label: 'nav.representatives', roles: ['Administrator', 'PortAuthorityOfficer'] },
    { route: '/qualifications', label: 'nav.qualifications', roles: ['Administrator', 'LogisticsOperator'] },
    { route: '/physical-resources', label: 'nav.physicalResources', roles: ['Administrator', 'LogisticsOperator'] },
    { route: '/scheduling/daily', label: 'nav.dailyScheduling', roles: ['Administrator', 'LogisticsOperator'] },
    { route: '/operations', label: 'nav.operations', roles: ['Administrator', 'PortAuthorityOfficer', 'LogisticsOperator', 'PortOperationsSupervisor'] },
    { route: '/3d-viewer', label: 'nav.viewer3d', roles: ['Administrator', 'LogisticsOperator', 'PortAuthorityOfficer', 'PortOperationsSupervisor', 'ShippingAgentRepresentative'] }
  ];

  constructor(
    private translate: TranslateService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.currentLang = this.translate.currentLang || 'en';

    const profile = this.authService.getUserProfile();
    if (profile) {
      this.userName = profile.name || 'User';
      this.userEmail = profile.email || '';
      this.userPicture = profile.picture || '';
    }

    const role = this.authService.getUserRole();
    if (role) {
      this.userRole = role;
    }
  }

  get filteredMenuItems() {
    return this.menuItems.filter(item => item.roles.includes(this.userRole));
  }

  switchLanguage(lang: string): void {
    this.currentLang = lang;
    this.translate.use(lang);
    localStorage.setItem('language', lang);
  }

  logout(): void {
    this.authService.logout();
  }
}
