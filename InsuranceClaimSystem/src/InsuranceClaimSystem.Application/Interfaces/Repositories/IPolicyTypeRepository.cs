using InsuranceClaimSystem.Domain.Entities;
using System.Linq.Expressions;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IPolicyTypeRepository : IRepository<PolicyType>
{
    Task<IEnumerable<PolicyType>> GetActivePolicyTypesAsync();
}