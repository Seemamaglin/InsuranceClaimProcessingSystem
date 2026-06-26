using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class PaymentRepository : Repository<ClaimPayment>, IPaymentRepository
{
    public PaymentRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<ClaimPayment>> GetByClaimIdAsync(Guid claimId)
    {
        return await _dbContext.ClaimPayments
            .AsNoTracking()
            .Where(p => p.ClaimId == claimId)
            .ToListAsync();
    }

    public async Task<ClaimPayment?> GetByIdempotencyKeyAsync(Guid idempotencyKey)
    {
        return await _dbContext.ClaimPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);
    }

    public async Task<ClaimPayment?> GetByPaymentIntentIdAsync(string paymentIntentId)
    {
        return await _dbContext.ClaimPayments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);
    }

    public async Task<IEnumerable<ClaimPayment>> GetPendingPaymentsAsync()
    {
        return await _dbContext.ClaimPayments
            .AsNoTracking()
            .Where(p => p.PaymentStatus == ClaimPaymentStatus.Pending)
            .ToListAsync();
    }
}