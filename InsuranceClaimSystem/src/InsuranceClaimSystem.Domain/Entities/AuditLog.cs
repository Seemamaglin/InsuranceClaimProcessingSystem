using System;
using InsuranceClaimSystem.Domain.Common;

namespace InsuranceClaimSystem.Domain.Entities
{
    // Inherits: Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt from BaseEntity
    public class AuditLog : BaseEntity
    {
        public Guid UserId { get; set; }
        
        public string Action { get; set; } = string.Empty; // e.g. "Claim.Approved"
        public string EntityType { get; set; } = string.Empty; // "Claim", "Document"
        public Guid EntityId { get; set; }
        
        public string? OldValues { get; set; } // JSON string
        public string? NewValues { get; set; } // JSON string
        
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public User User { get; set; }
    }
}
