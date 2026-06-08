using System;
using InsuranceClaimSystem.Domain.Common;

namespace InsuranceClaimSystem.Domain.Entities
{
    public class ThirdPartyClaimant : BaseEntity
    {
        public Guid ClaimId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;

        public string DamageDescription { get; set; } = string.Empty;
        public decimal EstimatedDamageAmount { get; set; }

        public string? PoliceReportNumber { get; set; }

        public Claim Claim { get; set; }
        
    }
}