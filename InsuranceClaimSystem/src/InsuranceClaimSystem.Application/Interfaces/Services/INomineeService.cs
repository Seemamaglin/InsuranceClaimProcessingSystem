using InsuranceClaimSystem.Application.DTOs.Nominees;
using InsuranceClaimSystem.Application.Common;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface INomineeService
{
    Task<Result<NomineeDto>> AddNomineeAsync(Guid policyId, Guid requestUserId, NomineeRequest request);
    Task<Result<List<NomineeDto>>> GetNomineesByPolicyAsync(Guid policyId);
}
