using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface INomineeRepository : IRepository<Nominee>
{
    Task<IEnumerable<Nominee>> GetByPolicyIdAsync(Guid policyId);
    Task<Nominee?> GetActiveNomineeByPolicyIdAsync(Guid policyId);
}