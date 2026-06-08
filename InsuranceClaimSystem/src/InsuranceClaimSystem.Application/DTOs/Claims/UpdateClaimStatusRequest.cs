using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Claims;

public class UpdateClaimStatusRequest
{
    public ClaimStatus NewStatus { get; set; }
    public string? RejectionReason { get; set; }
    public Guid ChangedByUserId { get; set; }
}