using System.Linq.Expressions;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IPolicyRepository : IRepository<Policy>
{
    Task<Policy?> GetByPolicyNumberAsync(string policyNumber);
    Task<IEnumerable<Policy>> GetByPolicyHolderIdAsync(Guid policyHolderId);
    Task<PagedResult<Policy>> GetPagedAsync(int page, int pageSize, Expression<Func<Policy, bool>>? predicate = null);
    Task<int> CountByStatusAsync(PolicyStatus status);
}