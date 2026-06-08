using System;
using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities
{
    // Inherits: Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt from BaseEntity
    public class ClaimWorkflowHistory : BaseEntity
    {
        public Guid ClaimId { get; set; }
        public Guid ChangedByUserId { get; set; }
        public WorkflowActionType ActionType { get; set; }
        
        public ClaimStatus? PreviousStatus { get; set; }
        public ClaimStatus? NewStatus { get; set; }
        
        public string Comments { get; set; } = string.Empty;
        
        public Claim Claim { get; set; }
        public User ChangedByUser { get; set; }


    }
}
