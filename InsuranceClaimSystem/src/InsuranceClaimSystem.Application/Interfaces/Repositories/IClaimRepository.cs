using System.Linq.Expressions;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IClaimRepository : IRepository<Claim>
{
    Task<Claim?> GetByIdWithDetailsAsync(Guid claimId);
    Task<PagedResult<Claim>> GetPagedAsync(int page, int pageSize, Expression<Func<Claim, bool>>? predicate = null);
    Task<bool> HasOpenClaimAsync(Guid policyId);
    Task<bool> HasMaturityClaimAsync(Guid policyId);
    Task<int> GetActiveClaimCountByReviewerAsync(Guid reviewerId);
    Task<Dictionary<Guid, int>> GetActiveClaimCountsForReviewersAsync(IEnumerable<Guid> reviewerIds);
    Task<int> CountByStatusAsync(ClaimStatus status);
    Task<Claim?> GetClaimByNumberAsync(string claimNumber);
}