using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<ClaimPayment>
{
    Task<IEnumerable<ClaimPayment>> GetByClaimIdAsync(Guid claimId);
    Task<ClaimPayment?> GetByIdempotencyKeyAsync(Guid idempotencyKey);
    Task<IEnumerable<ClaimPayment>> GetPendingPaymentsAsync();
}