using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Common;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class PolicyRepository : Repository<Policy>, IPolicyRepository
{
    public PolicyRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Policy?> GetByPolicyNumberAsync(string policyNumber)
    {
        return await _dbContext.Policies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber);
    }

    public async Task<IEnumerable<Policy>> GetByPolicyHolderIdAsync(Guid policyHolderId)
    {
        return await _dbContext.Policies
            .AsNoTracking()
            .Where(p => p.PolicyHolderId == policyHolderId)
            .ToListAsync();
    }

    public async Task<PagedResult<Policy>> GetPagedAsync(int page, int pageSize, Expression<Func<Policy, bool>>? predicate = null)
    {
        var query = _dbContext.Policies.AsNoTracking();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<Policy>.Create(items, totalCount, page, pageSize);
    }

    public async Task<int> CountByStatusAsync(PolicyStatus status)
    {
        return await _dbContext.Policies.CountAsync(p => p.Status == status);
    }
}