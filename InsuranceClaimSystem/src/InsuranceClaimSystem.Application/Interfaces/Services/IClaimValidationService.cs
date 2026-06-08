using InsuranceClaimSystem.Application.DTOs.Claims;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface IClaimValidationService
{
    Task<ClaimValidationResult> ValidateSubmissionAsync(SubmitClaimRequest dto, Guid policyHolderId);
    Task<decimal> CalculatePayoutAsync(decimal claimedAmount, Guid claimTypeId, Guid policyTypeId);
}

public class ClaimValidationResult
{
    public bool IsValid { get; set; }
    public bool IsLateIntimation { get; set; }
    public List<string> Errors { get; set; } = new();
    public decimal DeductibleAmount { get; set; }
    public decimal CoPayPercentage { get; set; }
}