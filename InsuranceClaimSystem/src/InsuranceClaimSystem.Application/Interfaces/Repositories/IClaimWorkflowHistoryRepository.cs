using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IClaimWorkflowHistoryRepository : IRepository<ClaimWorkflowHistory>
{
    Task<IEnumerable<ClaimWorkflowHistory>> GetByClaimIdAsync(Guid claimId);
    Task<ClaimWorkflowHistory?> GetLatestByClaimIdAsync(Guid claimId);
}