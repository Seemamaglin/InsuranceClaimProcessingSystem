import { Component, Input } from '@angular/core';
import { PolicyStatus } from '../../../models/enums/status.enums';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-policy-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="badge" [ngClass]="getBadgeClass()">
      {{ getStatusName() }}
    </span>
  `,
  styles: [`
    .badge {
      display: inline-flex;
      align-items: center;
      padding: 4px 10px;
      border-radius: 9999px;
      font-size: 12px;
      font-weight: 600;
      white-space: nowrap;
    }
    .badge-pending { background-color: lightgoldenrodyellow; color: darkgoldenrod; }
    .badge-active { background-color: lightgreen; color: darkgreen; }
    .badge-grace { background-color: lightsalmon; color: sienna; }
    .badge-lapsed { background-color: mistyrose; color: firebrick; }
    .badge-expired { background-color: gainsboro; color: dimgray; }
    .badge-rejected { background-color: mistyrose; color: firebrick; }
    .badge-cancelled { background-color: gainsboro; color: dimgray; }
    .badge-exhausted { background-color: lavender; color: indigo; }
  `]
})
export class PolicyStatusBadgeComponent {
  @Input() status!: PolicyStatus;

  getBadgeClass(): string {
    switch (this.status) {
      case PolicyStatus.PendingApproval: return 'badge-pending';
      case PolicyStatus.Active: return 'badge-active';
      case PolicyStatus.GracePeriod: return 'badge-grace';
      case PolicyStatus.Lapsed: return 'badge-lapsed';
      case PolicyStatus.Expired: return 'badge-expired';
      case PolicyStatus.Rejected: return 'badge-rejected';
      case PolicyStatus.Cancelled: return 'badge-cancelled';
      case PolicyStatus.CoverageExhausted: return 'badge-exhausted';
      default: return 'badge-pending';
    }
  }

  getStatusName(): string {
    switch (this.status) {
      case PolicyStatus.PendingApproval: return 'Pending Approval';
      case PolicyStatus.Active: return 'Active';
      case PolicyStatus.GracePeriod: return 'Grace Period';
      case PolicyStatus.Lapsed: return 'Lapsed';
      case PolicyStatus.Expired: return 'Expired';
      case PolicyStatus.Rejected: return 'Rejected';
      case PolicyStatus.Cancelled: return 'Cancelled';
      case PolicyStatus.CoverageExhausted: return 'Exhausted';
      default: return 'Unknown';
    }
  }
}
