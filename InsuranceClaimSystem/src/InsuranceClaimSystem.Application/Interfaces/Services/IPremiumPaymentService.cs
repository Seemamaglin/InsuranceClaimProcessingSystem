using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface IPremiumPaymentService
{
    Task<Result<PolicyPayment>> RecordFirstPremiumAsync(Guid policyId, decimal amount, string stripePaymentIntentId);
    Task<Result<PolicyPayment>> RecordPremiumPaymentAsync(Guid policyId, decimal amount, string stripePaymentIntentId);
    Task<Result<PolicyPayment>> GetLastPaymentAsync(Guid policyId);
}