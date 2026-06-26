export enum UserRole {
  Admin = 1,
  ClaimsManager = 2,
  ClaimReviewer = 3,
  FinanceOfficer = 4,
  PolicyHolder = 5
}

export enum RegistrationStatus {
  NA = 1,
  PendingEmailVerification = 2,
  PendingApproval = 3,
  Approved = 4,
  Rejected = 5,
  PendingKyc = 6,
  KycRejected = 7 
}

export interface User {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  lastLoginAt?: string;
}

export interface AccountProfile {
  id: string;
  fullName: string;
  email: string;
  username: string;
  phoneNumber: string;
  dateOfBirth?: string;
  lastLoginAt?: string;
  role: UserRole;
  specialization?: string;
  registrationStatus: RegistrationStatus;
  isActive: boolean;
  isFirstLogin: boolean;
  createdAt: string;
}


export interface AuthResponse {
  userId: string;
  email: string;
  username: string;
  firstName: string;
  lastName: string;
  token: string;
  refreshToken: string;
  refreshTokenExpiry: string;
  role: UserRole;
  isFirstLogin: boolean;
}