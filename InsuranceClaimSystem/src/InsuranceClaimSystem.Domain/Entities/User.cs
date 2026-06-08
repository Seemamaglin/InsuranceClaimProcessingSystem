using System;
using System.Collections.Generic;
using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities
{
    // Inherits: Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt from BaseEntity
    public class User : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? EmailVerifiedAt { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        
        public UserRole Role { get; set; }
        public Specialization? Specialization { get; set; }
        
        public RegistrationStatus RegistrationStatus { get; set; }
        public string? RegistrationRejectionReason { get; set; }
        
        public bool IsActive { get; set; }
        public bool IsFirstLogin { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutUntil { get; set; }

        public ICollection<Policy> Policies { get; set; }
        public ICollection<Claim> Claims { get; set; } //creates one to many relationships
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; }
        public ICollection<KYCDocument> KYCDocuments { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; }
    }
}
