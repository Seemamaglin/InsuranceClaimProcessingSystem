import { Component, Input } from '@angular/core';
import { ClaimStatus } from '../../../models/enums/status.enums';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-claim-status-badge',
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
    .badge-draft { background-color: ghostwhite; color: slategray; border: 1px solid lightgray; }
    .badge-submitted { background-color: lightblue; color: midnightblue; }
    .badge-review { background-color: lightgoldenrodyellow; color: darkgoldenrod; }
    .badge-pending { background-color: lightpink; color: crimson; }
    .badge-approved { background-color: lightgreen; color: darkgreen; }
    .badge-rejected { background-color: mistyrose; color: firebrick; }
    .badge-closed { background-color: gainsboro; color: dimgray; }
  `]
})
export class ClaimStatusBadgeComponent {
  @Input() status!: ClaimStatus;

  getBadgeClass(): string {
    switch (this.status) {
      case ClaimStatus.Draft: return 'badge-draft';
      case ClaimStatus.Submitted: return 'badge-submitted';
      case ClaimStatus.UnderReview: return 'badge-review';
      case ClaimStatus.DocumentsPending: return 'badge-pending';
      case ClaimStatus.Approved: return 'badge-approved';
      case ClaimStatus.Rejected: return 'badge-rejected';
      case ClaimStatus.Closed: return 'badge-closed';
      default: return 'badge-draft';
    }
  }

  getStatusName(): string {
    switch (this.status) {
      case ClaimStatus.Draft: return 'Draft';
      case ClaimStatus.Submitted: return 'Submitted';
      case ClaimStatus.UnderReview: return 'Under Review';
      case ClaimStatus.DocumentsPending: return 'Action Required';
      case ClaimStatus.Approved: return 'Approved';
      case ClaimStatus.Rejected: return 'Rejected';
      case ClaimStatus.Closed: return 'Closed';
      default: return 'Unknown';
    }
  }
}
