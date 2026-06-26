using System;
using System.Collections.Generic;
using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities
{
    // Inherits: Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt from BaseEntity
    public class Claim : BaseEntity
    {
        public string ClaimNumber { get; set; } = string.Empty;
        public Guid PolicyId { get; set; }
        public Guid ClaimTypeId { get; set; }
        public Guid ClaimantId { get; set; }
        
        public Guid? AssignedReviewerId { get; set; }
        public Guid? AssignedManagerId { get; set; }
        
        public Guid? NomineeId { get; set; }


        public ClaimantType ClaimantType { get; set; }
        public DateTime? IncidentDate { get; set; }
        public string IncidentDescription { get; set; } = string.Empty;
        public string IncidentLocation { get; set; } = string.Empty;
        public DateTime? IntimationDate { get; set; }
        public bool IsLateIntimation { get; set; } = false;
        
        public decimal ClaimedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public decimal DeductibleAmount { get; set; } = 0;
        public decimal CoPayPercentage { get; set; } = 0;
        public decimal FinalPayableAmount { get; set; } = 0;
        
        public ClaimStatus Status { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? ResolvedAt { get; set; }


        public PaymentRecipientType PaymentRecipientType { get; set; }
        public string? RecipientName { get; set; } 
        public string? RecipientAccountNumber { get; set; } 
        public string? RecipientBankName { get; set; } 
        public string? RecipientIFSC { get; set; } 
       
        public Policy Policy { get; set; }
        public ClaimType ClaimType { get; set; }
        public User Claimant { get; set; }
        public User? AssignedReviewer { get; set; }
        public Nominee? Nominee { get; set; }

        public ICollection<Document> Documents { get; set;} = new List<Document>();
        public ICollection<ClaimNote> ClaimNotes { get; set; } = new List<ClaimNote>();
        public ICollection<ClaimPayment> ClaimPayments { get; set; } = new List<ClaimPayment>();
        public ICollection<ClaimWorkflowHistory> WorkflowHistories { get; set; } = new List<ClaimWorkflowHistory>();
        public ICollection<ThirdPartyClaimant> ThirdPartyClaimants { get; set; } = new List<ThirdPartyClaimant>();

    }
}
