using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Policies;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface IPolicyService
{
    Task<Result<PolicyTypeDto>> CreatePolicyTypeAsync(CreatePolicyTypeRequest request);
    Task<Result<PolicyResponse>> ApplyForPolicyAsync(Guid policyHolderId, ApplyForPolicyRequest request);
    Task<Result<PolicyResponse>> GetPolicyByIdAsync(Guid policyId);
    Task<Result<PolicyResponse>> GetPolicyByNumberAsync(string policyNumber);
    Task<Result<PolicyResponse>> UpdatePolicyAsync(UpdatePolicyRequest request);
    Task<Result<PolicyResponse>> ApprovePolicyAsync(Guid policyId);
    Task<Result<PolicyResponse>> RejectPolicyAsync(Guid policyId, string? reason);
    Task<Result<bool>> DeletePolicyAsync(Guid policyId);
    Task<Result<PagedResult<PolicyResponse>>> GetPoliciesAsync(int page, int pageSize);
    Task<Result<PagedResult<PolicyResponse>>> GetPoliciesByHolderAsync(Guid policyHolderId, int page, int pageSize);
    Task<Result<IEnumerable<PolicyTypeDto>>> GetPolicyTypesAsync();
}