import { AccountProfile, UserRole } from './user.model';

export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface RejectRegistrationRequest {
  reason: string;
}

export interface CreateStaffRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  email: string;
  userName: string;
  password?: string;
  phoneNumber: string;
  role: UserRole;
  specialization?: string;
}
