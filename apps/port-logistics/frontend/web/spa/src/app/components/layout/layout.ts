import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { Navbar } from '../navbar/navbar';
import { PrivacyPolicyNotification } from '../privacy-policy-notification/privacy-policy-notification';

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, Navbar, PrivacyPolicyNotification],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class Layout {
  // Unified layout component (US 3.1.2)
  // Navbar sempre visível + RouterOutlet para conteúdo dinâmico
  // Privacy Policy Notification (US 4.5.1)
}
