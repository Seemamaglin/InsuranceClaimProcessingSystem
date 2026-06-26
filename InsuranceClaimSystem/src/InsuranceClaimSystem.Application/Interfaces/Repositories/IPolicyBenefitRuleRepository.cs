using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IPolicyBenefitRuleRepository : IRepository<PolicyBenefitRule>
{
    Task<PolicyBenefitRule?> GetActiveRuleAsync(Guid policyTypeId, Guid claimTypeId);
}
