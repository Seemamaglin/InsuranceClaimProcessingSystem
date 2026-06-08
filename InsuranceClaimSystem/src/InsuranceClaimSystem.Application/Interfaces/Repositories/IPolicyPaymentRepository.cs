using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IPolicyPaymentRepository : IRepository<PolicyPayment>
{
    Task<IEnumerable<PolicyPayment>> GetByPolicyIdAsync(Guid policyId);
    Task<PolicyPayment?> GetLastPaymentAsync(Guid policyId);
}