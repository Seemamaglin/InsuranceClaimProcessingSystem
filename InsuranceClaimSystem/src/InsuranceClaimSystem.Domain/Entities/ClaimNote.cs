using System;
using InsuranceClaimSystem.Domain.Common;

namespace InsuranceClaimSystem.Domain.Entities
{
    // Inherits: Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt from BaseEntity
    public class ClaimNote : BaseEntity
    {
        public Guid ClaimId { get; set; }
        public Guid AuthorId { get; set; }

        public string Message { get; set; } = string.Empty;
        public bool IsInternalOnly { get; set; }

        public Claim Claim { get; set; }
        public User Author { get; set; }
    }
}
