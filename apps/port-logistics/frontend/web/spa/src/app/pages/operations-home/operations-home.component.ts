import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-operations-home',
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, TranslateModule],
  templateUrl: './operations-home.component.html',
  styleUrl: './operations-home.component.scss'
})
export class OperationsHomeComponent implements OnInit {
  tabs = [
    { route: '/operations/incident-types', label: 'operations.incidentTypes', icon: '⚠️', permission: 'incident-types' },
    { route: '/operations/incidents', label: 'operations.incidents', icon: '🚨', permission: 'incidents' },
    { route: '/operations/operation-plans', label: 'operations.operationPlans', icon: '📋', permission: 'operation-plans' },
    { route: '/operations/executions', label: 'operations.executions', icon: '⚙️', permission: 'vessel-visit-executions' },
    { route: '/operations/task-categories', label: 'operations.taskCategories', icon: '📂', permission: 'task-categories' },
    { route: '/operations/tasks', label: 'operations.tasks', icon: '✓', permission: 'complementary-tasks' }
  ];

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Redirect to first accessible tab if on base /operations route
    if (this.router.url === '/operations' || this.router.url === '/operations/') {
      const firstAccessibleTab = this.tabs.find(tab => this.authService.hasPermission(tab.permission));
      if (firstAccessibleTab) {
        this.router.navigate([firstAccessibleTab.route]);
      }
    }
  }

  get visibleTabs() {
    return this.tabs.filter(tab => this.authService.hasPermission(tab.permission));
  }
}
