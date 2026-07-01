import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { AccountProfile, UserRole } from '../../../models/user.model';
import { PaginatedList, CreateStaffRequest } from '../../../models/admin.model';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  private adminService = inject(AdminService);
  private fb = inject(FormBuilder);

  activeTab: 'pending' | 'users' = 'pending';
  selectedRoleFilter: UserRole | '' = '';

  pendingRegistrations: AccountProfile[] = [];
  allUsers: PaginatedList<AccountProfile> | null = null;
  
  isLoadingPending = false;
  isLoadingUsers = false;
  isSubmitting = false;

  showRejectModal = false;
  selectedUserIdToReject: string | null = null;
  rejectReason = '';

  showCreateStaffModal = false;

  staffForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    dateOfBirth: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    userName: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    phoneNumber: ['', Validators.required],
    role: [UserRole.ClaimReviewer, Validators.required],
    specialization: ['']
  });

  // Expose UserRole to template
  userRoleEnum = UserRole;

  ngOnInit() {
    this.loadPendingRegistrations();
    this.loadAllUsers();
  }

  setTab(tab: 'pending' | 'users') {
    this.activeTab = tab;
  }

  loadPendingRegistrations() {
    this.isLoadingPending = true;
    this.adminService.getPendingRegistrations().subscribe({
      next: (data) => {
        this.pendingRegistrations = data || [];
        this.isLoadingPending = false;
      },
      error: () => {
        this.pendingRegistrations = [];
        this.isLoadingPending = false;
      }
    });
  }

  onRoleFilterChange(event: Event) {
    const select = event.target as HTMLSelectElement;
    this.selectedRoleFilter = select.value ? Number(select.value) as UserRole : '';
    this.loadAllUsers();
  }

  loadAllUsers() {
    this.isLoadingUsers = true;
    const roleParam = this.selectedRoleFilter ? this.selectedRoleFilter : undefined;
    this.adminService.getAllUsers(1, 50, roleParam).subscribe({
      next: (data) => {
        this.allUsers = data;
        this.isLoadingUsers = false;
      },
      error: () => this.isLoadingUsers = false
    });
  }

  approve(userId: string) {
    if (confirm('Are you sure you want to approve this user?')) {
      this.adminService.approveRegistration(userId).subscribe({
        next: () => {
          this.loadPendingRegistrations();
          this.loadAllUsers();
        },
        error: (err) => alert('Error approving user: ' + err.error?.error)
      });
    }
  }

  openRejectModal(userId: string) {
    this.selectedUserIdToReject = userId;
    this.rejectReason = '';
    this.showRejectModal = true;
  }

  closeRejectModal() {
    this.selectedUserIdToReject = null;
    this.showRejectModal = false;
  }

  updateRejectReason(event: Event) {
    const input = event.target as HTMLInputElement;
    this.rejectReason = input.value;
  }

  confirmReject() {
    if (!this.selectedUserIdToReject || !this.rejectReason) {
      alert('Please provide a reason for rejection.');
      return;
    }

    this.adminService.rejectRegistration(this.selectedUserIdToReject, this.rejectReason).subscribe({
      next: () => {
        this.closeRejectModal();
        this.loadPendingRegistrations();
        this.loadAllUsers();
      },
      error: (err) => alert('Error rejecting user: ' + err.error?.error)
    });
  }

  openCreateStaffModal() {
    this.staffForm.reset({ role: UserRole.ClaimReviewer });
    this.showCreateStaffModal = true;
  }

  closeCreateStaffModal() {
    this.showCreateStaffModal = false;
  }

  onSubmitStaff() {
    if (this.staffForm.invalid) return;

    this.isSubmitting = true;
    const formValue = this.staffForm.value;
    const payload: CreateStaffRequest = {
      firstName: formValue.firstName!,
      lastName: formValue.lastName!,
      dateOfBirth: formValue.dateOfBirth!,
      email: formValue.email!,
      userName: formValue.userName!,
      password: formValue.password!,
      phoneNumber: formValue.phoneNumber!,
      role: Number(formValue.role!) as UserRole,
      specialization: formValue.specialization || undefined
    };

    this.adminService.createStaff(payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeCreateStaffModal();
        this.loadAllUsers();
      },
      error: (err) => {
        this.isSubmitting = false;
        alert('Error creating staff: ' + (err.error?.error || 'Unknown error'));
      }
    });
  }
}
