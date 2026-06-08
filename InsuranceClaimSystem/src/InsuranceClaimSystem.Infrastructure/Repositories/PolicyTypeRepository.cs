using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class PolicyTypeRepository : Repository<PolicyType>, IPolicyTypeRepository
{
    public PolicyTypeRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<PolicyType>> GetActivePolicyTypesAsync()
    {
        return await _dbContext.PolicyTypes
            .AsNoTracking()
            .Where(pt => pt.IsActive)
            .ToListAsync();
    }
}