import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { NotificationBellComponent } from '../../shared/components/notification-bell/notification-bell.component';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-staff-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, NotificationBellComponent],
  templateUrl: './staff-layout.component.html',
  styleUrl: './staff-layout.component.css'
})
export class StaffLayoutComponent {
  userRole = '';

  constructor(private authService: AuthService) {
    const role = this.authService.currentUserValue?.role;
    // Map numerical enum back to string for UI comparison
    if (role === 1) this.userRole = 'Admin';
    else if (role === 2) this.userRole = 'ClaimsManager';
    else if (role === 3) this.userRole = 'ClaimReviewer';
    else if (role === 4) this.userRole = 'FinanceOfficer';
    else if (role === 5) this.userRole = 'PolicyHolder';
  }

  logout() {
    this.authService.logout();
    window.location.href = '/login';
  }
}
