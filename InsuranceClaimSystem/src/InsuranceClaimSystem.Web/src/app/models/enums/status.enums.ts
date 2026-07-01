export enum ClaimStatus {
  Draft = 1,
  Submitted = 2,
  UnderReview = 3,
  DocumentsPending = 4,
  Approved = 5,
  Rejected = 6,
  Closed = 7
}

export enum PolicyStatus {
  PendingApproval = 1,
  Active = 2,
  GracePeriod = 3,
  Lapsed = 4,
  Expired = 5,
  Rejected = 6,
  Cancelled = 7,
  CoverageExhausted = 8
}

export enum VerificationStatus {
  Pending = 1,
  Verified = 2,
  Rejected = 3
}
