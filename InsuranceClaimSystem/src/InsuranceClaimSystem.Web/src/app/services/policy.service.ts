import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PolicyType, ApplyForPolicyRequest } from '../models/policy.model';

@Injectable({
  providedIn: 'root'
})
export class PolicyService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl; 

  getPolicyTypes(): Observable<PolicyType[]> {
    // Extract the policy array from inside the wrapper's .data property
    return this.http.get<{data: PolicyType[]}>(`${this.baseUrl}/policy-types`)
      .pipe(map(res => res.data));
  }

  applyForPolicy(request: ApplyForPolicyRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/policies/apply`, request);
  }
}