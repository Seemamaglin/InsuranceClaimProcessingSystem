using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Claims;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<Result<(string PaymentIntentId, decimal FinalPayableAmount)>> CreatePaymentIntentAsync(Guid claimId);
    Task<Result<(bool Success, decimal FinalPayableAmount)>> ConfirmPaymentAsync(Guid claimId, string paymentIntentId);
    Task<Result<ClaimPaymentDto?>> GetPaymentByClaimIdAsync(Guid claimId);
}