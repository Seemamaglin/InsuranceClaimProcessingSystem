import { Component, OnInit, inject } from '@angular/core';
import { PolicyService } from '../services/policy.service';
import { PolicyType } from '../models/policy.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-policy-browser',
  standalone: true,
  templateUrl: './policy-browser.component.html',
  styleUrls: ['./policy-browser.component.css']
})

export class PolicyBrowserComponent implements OnInit {
  private policyService = inject(PolicyService);
  private router = inject(Router);

  policyTypes: PolicyType[] = [];
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.policyService.getPolicyTypes().subscribe({
      next: (data) => {
        this.policyTypes = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = 'Failed to load policies. Please try again later.';
        this.isLoading = false;
      }
    });
  }

    apply(policyTypeId: string) {
    this.router.navigate(['/apply-policy', policyTypeId]);
  }
}