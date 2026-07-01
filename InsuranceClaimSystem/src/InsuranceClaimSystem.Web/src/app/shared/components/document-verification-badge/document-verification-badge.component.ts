import { Component, Input } from '@angular/core';
import { VerificationStatus } from '../../../models/enums/status.enums';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-document-verification-badge',
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
    .badge-verified { background-color: lightgreen; color: darkgreen; }
    .badge-rejected { background-color: mistyrose; color: firebrick; }
  `]
})
export class DocumentVerificationBadgeComponent {
  @Input() status!: VerificationStatus;

  getBadgeClass(): string {
    switch (this.status) {
      case VerificationStatus.Pending: return 'badge-pending';
      case VerificationStatus.Verified: return 'badge-verified';
      case VerificationStatus.Rejected: return 'badge-rejected';
      default: return 'badge-pending';
    }
  }

  getStatusName(): string {
    switch (this.status) {
      case VerificationStatus.Pending: return 'Pending';
      case VerificationStatus.Verified: return 'Verified';
      case VerificationStatus.Rejected: return 'Rejected';
      default: return 'Unknown';
    }
  }
}
