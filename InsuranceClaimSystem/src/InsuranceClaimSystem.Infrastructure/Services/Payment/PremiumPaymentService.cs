using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class PremiumPaymentService : IPremiumPaymentService
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IPolicyPaymentRepository _policyPaymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;
    private readonly ILogger<PremiumPaymentService> _logger;

    public PremiumPaymentService(
        IPolicyRepository policyRepository,
        IPolicyPaymentRepository policyPaymentRepository,
        IUnitOfWork unitOfWork,
        IStripeService stripeService,
        ILogger<PremiumPaymentService> logger)
    {
        _policyRepository = policyRepository;
        _policyPaymentRepository = policyPaymentRepository;
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<Result<PolicyPayment>> RecordFirstPremiumAsync(
        Guid policyId, 
        decimal amount, 
        string stripePaymentIntentId)
    {
        _logger.LogInformation("Recording first premium for policy {PolicyId}", policyId);
        try
        {
            var policy = await _policyRepository.GetByIdAsync(policyId);
            if (policy == null)
            {
                _logger.LogWarning("Policy {PolicyId} not found", policyId);
                return Result<PolicyPayment>.Failure(
                    Error.NotFound("PolicyNotFound", "Policy not found."));
            }

            var payment = BuildPolicyPayment(policyId, amount, stripePaymentIntentId);

            await _policyPaymentRepository.AddAsync(payment);

            policy.LastPremiumPaidDate = DateTime.UtcNow;
            policy.NextPremiumDueDate = CalculateNextPremiumDueDate(DateTime.UtcNow, policy.PremiumFrequency);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("First premium recorded successfully for policy {PolicyId}", policyId);
            return Result<PolicyPayment>.Success(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording first premium for policy {PolicyId}", policyId);
            return Result<PolicyPayment>.Failure(
                Error.Validation("RecordPremiumFailed", "An error occurred while recording the premium payment."));
        }
    }

    public async Task<Result<PolicyPayment>> RecordPremiumPaymentAsync(
        Guid policyId, 
        decimal amount, 
        string stripePaymentIntentId)
    {
        _logger.LogInformation("Recording premium payment for policy {PolicyId}", policyId);
        try
        {
            var policy = await _policyRepository.GetByIdAsync(policyId);
            if (policy == null)
            {
                _logger.LogWarning("Policy {PolicyId} not found", policyId);
                return Result<PolicyPayment>.Failure(
                    Error.NotFound("PolicyNotFound", "Policy not found."));
            }

            var payment = BuildPolicyPayment(policyId, amount, stripePaymentIntentId);

            await _policyPaymentRepository.AddAsync(payment);

            policy.LastPremiumPaidDate = DateTime.UtcNow;
            policy.NextPremiumDueDate = CalculateNextPremiumDueDate(DateTime.UtcNow, policy.PremiumFrequency);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Premium payment recorded successfully for policy {PolicyId}", policyId);
            return Result<PolicyPayment>.Success(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording premium for policy {PolicyId}", policyId);
            return Result<PolicyPayment>.Failure(
                Error.Validation("RecordPremiumFailed", "An error occurred while recording the premium payment."));
        }
    }

    public async Task<Result<PolicyPayment>> PayPremiumAsync(Guid policyHolderId, PayPremiumRequest request)
    {
        _logger.LogInformation("Processing premium payment for policy {PolicyId} by holder {PolicyHolderId}", request.PolicyId, policyHolderId);
        try
        {
            var policy = await _policyRepository.GetByIdAsync(request.PolicyId);
            if (policy == null)
                return Result<PolicyPayment>.Failure(Error.NotFound("PolicyNotFound", "Policy not found."));

            if (policy.PolicyHolderId != policyHolderId)
                return Result<PolicyPayment>.Failure(Error.Unauthorized("Unauthorized", "You are not authorized to pay for this policy."));

            if (policy.Status != PolicyStatus.Active)
                return Result<PolicyPayment>.Failure(Error.Validation("PolicyNotActive", "Cannot pay premium for an inactive policy."));

            if (request.Amount != policy.PremiumAmount)
                return Result<PolicyPayment>.Failure(Error.Validation("AmountMismatch", $"Premium amount must be exactly {policy.PremiumAmount}."));

            var simulatedStripeIntentId = $"pi_sim_{Guid.NewGuid():N}";
            
            var payment = BuildPolicyPayment(request.PolicyId, request.Amount, simulatedStripeIntentId);
            await _policyPaymentRepository.AddAsync(payment);

            policy.LastPremiumPaidDate = DateTime.UtcNow;
            var baseDate = policy.NextPremiumDueDate > DateTime.UtcNow 
                ? policy.NextPremiumDueDate 
                : DateTime.UtcNow;
            policy.NextPremiumDueDate = CalculateNextPremiumDueDate(baseDate, policy.PremiumFrequency);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Premium paid successfully for policy {PolicyId}", request.PolicyId);
            return Result<PolicyPayment>.Success(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing premium for policy {PolicyId}", request.PolicyId);
            return Result<PolicyPayment>.Failure(Error.Validation("PayPremiumFailed", "An error occurred while paying the premium."));
        }
    }

    public async Task<Result<PolicyPayment>> GetLastPaymentAsync(Guid policyId)
    {
        _logger.LogInformation("Getting last payment for policy {PolicyId}", policyId);
        try
        {
            var payment = await _policyPaymentRepository.GetLastPaymentAsync(policyId);
            if (payment == null)
            {
                _logger.LogWarning("No payment found for policy {PolicyId}", policyId);
                return Result<PolicyPayment>.Failure(
                    Error.NotFound("PaymentNotFound", "No payment found for this policy."));
            }

            _logger.LogInformation("Last payment retrieved for policy {PolicyId}", policyId);
            return Result<PolicyPayment>.Success(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving last payment for policy {PolicyId}", policyId);
            return Result<PolicyPayment>.Failure(
                Error.Validation("GetPaymentFailed", "An error occurred while retrieving the payment."));
        }
    }

    private static PolicyPayment BuildPolicyPayment(
        Guid policyId, 
        decimal amount, 
        string stripePaymentIntentId)
    {
        return new PolicyPayment
        {
            PolicyId = policyId,
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            Status = PolicyPaymentStatus.Paid,
            StripePaymentIntentId = stripePaymentIntentId
        };
    }

    private static DateTime CalculateNextPremiumDueDate(DateTime fromDate, PremiumFrequency frequency)
    {
        return frequency switch
        {
            PremiumFrequency.Monthly => fromDate.AddMonths(1),
            PremiumFrequency.Quarterly => fromDate.AddMonths(3),
            PremiumFrequency.HalfYearly => fromDate.AddMonths(6),
            PremiumFrequency.Annually => fromDate.AddYears(1),
            _ => fromDate.AddMonths(1)
        };
    }
}