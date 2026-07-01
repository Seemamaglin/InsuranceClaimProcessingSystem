import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { AccountProfile, UserRole } from '../models/user.model';
import { PaginatedList, CreateStaffRequest, RejectRegistrationRequest } from '../models/admin.model';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin`;

  getPendingRegistrations(): Observable<AccountProfile[]> {
    return this.http.get<{data: AccountProfile[]}>(`${this.apiUrl}/registrations/pending`)
      .pipe(map(res => res.data));
  }

  approveRegistration(userId: string): Observable<{ success: boolean, message: string }> {
    return this.http.post<{ success: boolean, message: string }>(`${this.apiUrl}/registrations/${userId}/approve`, {});
  }

  rejectRegistration(userId: string, reason: string): Observable<{ success: boolean, message: string }> {
    const payload: RejectRegistrationRequest = { reason };
    return this.http.post<{ success: boolean, message: string }>(`${this.apiUrl}/registrations/${userId}/reject`, payload);
  }

  getAllUsers(page: number = 1, pageSize: number = 10, role?: UserRole): Observable<PaginatedList<AccountProfile>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (role) {
      params = params.set('role', role.toString());
    }

    return this.http.get<{data: PaginatedList<AccountProfile>}>(`${this.apiUrl}/users`, { params })
      .pipe(map(res => res.data));
  }

  createStaff(payload: CreateStaffRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/users/create-staff`, payload);
  }
}
