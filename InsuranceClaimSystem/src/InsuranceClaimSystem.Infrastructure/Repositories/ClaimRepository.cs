using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Common;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class ClaimRepository : Repository<Claim>, IClaimRepository
{
    public ClaimRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Claim?> GetByIdWithDetailsAsync(Guid claimId)
    {
        return await _dbContext.Claims
            .Include(c => c.Policy)
            .Include(c => c.ClaimType)
            .Include(c => c.Claimant)
            .Include(c => c.AssignedReviewer)
            .Include(c => c.Documents)
            .Include(c => c.ClaimNotes)
            .Include(c => c.WorkflowHistories)
            .Include(c => c.ClaimPayments)
            .Include(c => c.Nominee)
            .Include(c => c.ThirdPartyClaimants)
            .FirstOrDefaultAsync(c => c.Id == claimId);
    }

    public async Task<PagedResult<Claim>> GetPagedAsync(int page, int pageSize, Expression<Func<Claim, bool>>? predicate = null)
    {
        var query = _dbContext.Claims.AsNoTracking();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<Claim>.Create(items, totalCount, page, pageSize);
    }

    public async Task<bool> HasOpenClaimAsync(Guid policyId)
    {
        var openStatuses = new[] { ClaimStatus.Submitted, ClaimStatus.UnderReview, ClaimStatus.DocumentsPending };
        return await _dbContext.Claims
            .AnyAsync(c => c.PolicyId == policyId && openStatuses.Contains(c.Status));
    }

    public async Task<bool> HasMaturityClaimAsync(Guid policyId)
    {
        return await _dbContext.Claims
            .Include(c => c.ClaimType)
            .AnyAsync(c => c.PolicyId == policyId && c.ClaimType.IsMaturityClaim);
    }

    public async Task<int> GetActiveClaimCountByReviewerAsync(Guid reviewerId)
    {
        var activeStatuses = new[] { ClaimStatus.Submitted, ClaimStatus.UnderReview, ClaimStatus.DocumentsPending };
        return await _dbContext.Claims
            .CountAsync(c => c.AssignedReviewerId == reviewerId && activeStatuses.Contains(c.Status));
    }

    public async Task<int> CountByStatusAsync(ClaimStatus status)
    {
        return await _dbContext.Claims.CountAsync(c => c.Status == status);
    }

    public async Task<Claim?> GetClaimByNumberAsync(string claimNumber)
    {
        return await _dbContext.Claims
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClaimNumber == claimNumber);
    }
}