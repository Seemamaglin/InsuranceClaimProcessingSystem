import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PolicyService } from '../services/policy.service';
import { ApplyForPolicyRequest, PremiumFrequency } from '../models/policy.model';

@Component({
  selector: 'app-apply-policy',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './apply-policy.component.html',
  styleUrls: ['./apply-policy.component.css']
})
export class ApplyPolicyComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private policyService = inject(PolicyService);

  policyTypeId = '';
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  // 1. Dropdown options for how much premium they want to pay
  premiumOptions = [
    { amount: 1000, label: '₹1,000 (Basic)' },
    { amount: 5000, label: '₹5,000 (Most Popular ✨)' },
    { amount: 10000, label: '₹10,000 (Premium)' }
  ];

  // 2. Define the Reactive Form
  applyForm = this.fb.group({
    // Coverage is a free-type number input
    coverageAmount: [500000, [Validators.required, Validators.min(100000)]],
    // Premium Amount uses the dropdown, defaulting to 5000
    premiumAmount: [5000, [Validators.required]],
    // Frequency defaults to 4 (Yearly)
    premiumFrequency: [4, [Validators.required]] 
  });

  ngOnInit() {
    // 3. Extract the Policy ID from the URL (e.g. /apply-policy/12345)
    this.policyTypeId = this.route.snapshot.paramMap.get('id') || '';
  }

  onSubmit() {
    if (this.applyForm.invalid) return;
    this.isLoading = true;
    this.errorMessage = '';     
    this.successMessage = '';  

    // 4. Construct the API payload using the new dropdown value
    const request: ApplyForPolicyRequest = {
      policyTypeId: this.policyTypeId,
      startDate: new Date().toISOString(),
      coverageAmount: this.applyForm.value.coverageAmount!,
      premiumAmount: Number(this.applyForm.value.premiumAmount!), // Grabbed from dropdown!
      premiumFrequency: Number(this.applyForm.value.premiumFrequency!),
      nominees: [] // Skipping nominees for now to keep it simple!
    };

    // 5. Send to backend!
    this.policyService.applyForPolicy(request).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.successMessage = 'Application Successful! (Next step: Stripe Payment)';
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Failed to apply.';
      }
    });
  }
}