using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class NomineeRepository : Repository<Nominee>, INomineeRepository
{
    public NomineeRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Nominee>> GetByPolicyIdAsync(Guid policyId)
    {
        return await _dbContext.Nominees
            .AsNoTracking()
            .Where(n => n.PolicyId == policyId)
            .ToListAsync();
    }

    public async Task<Nominee?> GetActiveNomineeByPolicyIdAsync(Guid policyId)
    {
        return await _dbContext.Nominees
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.PolicyId == policyId && n.IsActive);
    }
}