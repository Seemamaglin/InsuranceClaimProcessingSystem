using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Claims;

public class ClaimDetailDto
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Guid ClaimTypeId { get; set; }
    public string ClaimTypeName { get; set; } = string.Empty;
    public Guid ClaimantId { get; set; }
    public string ClaimantName { get; set; } = string.Empty;
    public Guid? AssignedReviewerId { get; set; }
    public string? AssignedReviewerName { get; set; }
    public Guid? NomineeId { get; set; }
    public ClaimantType ClaimantType { get; set; }
    public DateTime? IncidentDate { get; set; }
    public DateTime? IntimationDate { get; set; }
    public bool IsLateIntimation { get; set; }
    public decimal ClaimedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal DeductibleAmount { get; set; }
    public decimal CoPayPercentage { get; set; }
    public decimal? FinalPayableAmount { get; set; }
    public ClaimStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<DocumentDto> Documents { get; set; } = new();
    public List<ClaimWorkflowHistoryDto> WorkflowHistory { get; set; } = new();
    public List<ClaimNoteDto> Notes { get; set; } = new();
    public List<ClaimPaymentDto> Payments { get; set; } = new();
    public List<ThirdPartyClaimantDto> ThirdParties { get; set; } = new();
}