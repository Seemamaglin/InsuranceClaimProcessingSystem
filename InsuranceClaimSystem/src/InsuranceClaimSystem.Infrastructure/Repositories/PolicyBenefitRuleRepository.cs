using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class PolicyBenefitRuleRepository : Repository<PolicyBenefitRule>, IPolicyBenefitRuleRepository
{
    public PolicyBenefitRuleRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<PolicyBenefitRule?> GetActiveRuleAsync(Guid policyTypeId, Guid claimTypeId)
    {
        return await _dbContext.PolicyBenefitRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.PolicyTypeId == policyTypeId
                && r.ClaimTypeId == claimTypeId && r.IsActive);
    }
}
