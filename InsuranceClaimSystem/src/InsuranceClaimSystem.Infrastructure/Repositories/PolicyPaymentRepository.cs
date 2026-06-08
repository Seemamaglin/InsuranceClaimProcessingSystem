using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class PolicyPaymentRepository : Repository<PolicyPayment>, IPolicyPaymentRepository
{
    public PolicyPaymentRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<PolicyPayment>> GetByPolicyIdAsync(Guid policyId)
    {
        return await _dbContext.PolicyPayments
            .AsNoTracking()
            .Where(p => p.PolicyId == policyId)
            .ToListAsync();
    }

    public async Task<PolicyPayment?> GetLastPaymentAsync(Guid policyId)
    {
        return await _dbContext.PolicyPayments
            .AsNoTracking()
            .Where(p => p.PolicyId == policyId)
            .OrderByDescending(p => p.PaymentDate)
            .FirstOrDefaultAsync();
    }
}