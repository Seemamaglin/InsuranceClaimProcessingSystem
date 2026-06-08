using System;
using System.Collections.Generic;
using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities
{
    // Inherits: Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt from BaseEntity
    public class PolicyType : BaseEntity
    {
        public string TypeName { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty;
        public BenefitType DefaultBenefitType { get; set; }
        public bool AllowsNomineeClaim { get; set; }
        public bool AllowsThirdPartyClaim { get; set; }
        public decimal DefaultCoverageAmount { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
