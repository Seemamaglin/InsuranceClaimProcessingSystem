using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Claims;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface IClaimService
{
    Task<Result<ClaimDetailDto>> SubmitClaimAsync(SubmitClaimRequest request);
    Task<Result<ClaimDetailDto>> GetClaimByIdAsync(Guid claimId);
    Task<Result<ClaimDetailDto>> GetClaimByNumberAsync(string claimNumber);
    Task<Result<bool>> UpdateStatusAsync(Guid claimId, UpdateClaimStatusRequest request);
    Task<Result<bool>> AssignReviewerAsync(AssignReviewerRequest request);
    Task<Result<bool>> AutoAssignReviewerAsync(Guid claimId);
    Task<Result<PagedResult<ClaimDto>>> GetClaimsAsync(int page, int pageSize);
    Task<Result<PagedResult<ClaimDto>>> GetClaimsByPolicyAsync(Guid policyId, int page, int pageSize);
    Task<Result<PagedResult<ClaimDto>>> GetClaimsByReviewerAsync(Guid reviewerId, int page, int pageSize);
    Task<Result<decimal>> CalculatePayoutAsync(Guid claimId);
}