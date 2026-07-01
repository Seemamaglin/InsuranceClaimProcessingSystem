import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bell-container" (click)="toggleDropdown()">
      <span class="bell-icon">🔔</span>
      @if (unreadCount > 0) {
        <span class="badge">{{ unreadCount }}</span>
      }

      @if (isOpen) {
        <div class="dropdown">
          <div class="dropdown-header">
            <h4>Notifications</h4>
            <button class="mark-read">Mark all as read</button>
          </div>
          <div class="dropdown-body">
            <div class="empty-state">
              No new notifications
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .bell-container {
      position: relative;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: ghostwhite;
      transition: background 0.2s;
    }
    .bell-container:hover {
      background: lightgray;
    }
    .bell-icon {
      font-size: 20px;
    }
    .badge {
      position: absolute;
      top: -2px;
      right: -2px;
      background: crimson;
      color: white;
      font-size: 10px;
      font-weight: bold;
      padding: 2px 6px;
      border-radius: 10px;
      border: 2px solid white;
    }
    .dropdown {
      position: absolute;
      top: 120%;
      right: 0;
      width: 320px;
      background: white;
      border-radius: 12px;
      box-shadow: 0 10px 25px rgba(0,0,0,0.1);
      border: 1px solid lightgray;
      z-index: 100;
      overflow: hidden;
      cursor: default;
    }
    .dropdown-header {
      padding: 16px;
      border-bottom: 1px solid ghostwhite;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .dropdown-header h4 {
      margin: 0;
      color: midnightblue;
      font-size: 16px;
    }
    .mark-read {
      background: none;
      border: none;
      color: royalblue;
      font-size: 12px;
      cursor: pointer;
    }
    .mark-read:hover {
      text-decoration: underline;
    }
    .dropdown-body {
      max-height: 360px;
      overflow-y: auto;
    }
    .empty-state {
      padding: 32px;
      text-align: center;
      color: slategray;
      font-size: 14px;
    }
  `]
})
export class NotificationBellComponent {
  isOpen = false;
  unreadCount = 0; // Will be dynamic in Phase 7

  toggleDropdown() {
    this.isOpen = !this.isOpen;
  }
}
