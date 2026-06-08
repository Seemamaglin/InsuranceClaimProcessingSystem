using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IClaimTypeRepository : IRepository<ClaimType>
{
    Task<IEnumerable<ClaimType>> GetByPolicyTypeIdAsync(Guid policyTypeId);
    Task<ClaimType?> GetByTypeNameAsync(string typeName);
}