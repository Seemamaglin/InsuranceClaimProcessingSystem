using System;
using System.Collections.Generic;
using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities
{
    // Inherits: Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt from BaseEntity
    public class Policy : BaseEntity
    {
        public string PolicyNumber { get; set; } = string.Empty;
        
        public Guid PolicyHolderId { get; set; }
        public Guid PolicyTypeId { get; set; }
        
        public PolicyStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        public decimal CoverageAmount { get; set; }
        public decimal RemainingCoverageAmount { get; set; }
        
        public decimal PremiumAmount { get; set; }
        public PremiumFrequency PremiumFrequency { get; set; }
        public DateTime NextPremiumDueDate { get; set; }
        public DateTime? LastPremiumPaidDate { get; set; }
        public int GracePeriodDays { get; set; }
        
        public string? PolicyDocumentUrl { get; set; }
        
        public string? RejectionReason { get; set; }
        public DateTime? LapsedAt { get; set; }

        public User PolicyHolder { get; set;}
        public PolicyType PolicyType { get; set;}
        public ICollection<Claim> Claims { get; set; }
        public ICollection<Nominee> Nominees { get; set; }
        public ICollection<PolicyPayment> PolicyPayments { get; set; } = new List<PolicyPayment>();
        public HealthRecord? HealthRecord { get; set; }
    }
}
