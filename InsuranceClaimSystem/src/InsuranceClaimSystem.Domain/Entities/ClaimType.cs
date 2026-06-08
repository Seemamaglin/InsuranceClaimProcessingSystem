using System;
using System.Text.Json;
using System.Collections.Generic;
using InsuranceClaimSystem.Domain.Common;

namespace InsuranceClaimSystem.Domain.Entities
{
    // Inherits: Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt from BaseEntity
    public class ClaimType : BaseEntity
    {
        public string TypeName { get; set; } = string.Empty;    
        public bool IsMaturityClaim { get; set; }
        public bool IsFixedBenefit { get; set; }
        public JsonDocument RequiredDocuments { get; set; }    
        public Guid PolicyTypeId { get; set; }
        public PolicyType PolicyType { get; set; } 
    }
}
