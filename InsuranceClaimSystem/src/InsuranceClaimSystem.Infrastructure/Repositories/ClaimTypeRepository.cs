using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class ClaimTypeRepository : Repository<ClaimType>, IClaimTypeRepository
{
    public ClaimTypeRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<ClaimType>> GetByPolicyTypeIdAsync(Guid policyTypeId)
    {
        return await _dbContext.ClaimTypes
            .AsNoTracking()
            .Where(ct => ct.PolicyTypeId == policyTypeId)
            .ToListAsync();
    }

    public async Task<ClaimType?> GetByTypeNameAsync(string typeName)
    {
        return await _dbContext.ClaimTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.TypeName.ToLower() == typeName.ToLower());
    }
}