using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface IPremiumPaymentService
{
    Task<Result<PolicyPayment>> RecordFirstPremiumAsync(Guid policyId, decimal amount, string stripePaymentIntentId);
    Task<Result<PolicyPayment>> RecordPremiumPaymentAsync(Guid policyId, decimal amount, string stripePaymentIntentId);
    Task<Result<PolicyPayment>> GetLastPaymentAsync(Guid policyId);
    Task<Result<PolicyPayment>> PayPremiumAsync(Guid policyHolderId, PayPremiumRequest request);
}