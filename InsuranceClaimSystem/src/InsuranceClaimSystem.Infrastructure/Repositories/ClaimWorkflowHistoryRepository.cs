using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class ClaimWorkflowHistoryRepository : Repository<ClaimWorkflowHistory>, IClaimWorkflowHistoryRepository
{
    public ClaimWorkflowHistoryRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<ClaimWorkflowHistory>> GetByClaimIdAsync(Guid claimId)
    {
        return await _dbContext.WorkFlowHistory
            .AsNoTracking()
            .Where(w => w.ClaimId == claimId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<ClaimWorkflowHistory?> GetLatestByClaimIdAsync(Guid claimId)
    {
        return await _dbContext.WorkFlowHistory
            .AsNoTracking()
            .Where(w => w.ClaimId == claimId)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync();
    }
}