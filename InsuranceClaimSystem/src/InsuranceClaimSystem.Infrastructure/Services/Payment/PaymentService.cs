using Microsoft.Extensions.Logging;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Domain.Exceptions;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IClaimRepository _claimRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly IClaimWorkflowHistoryRepository _workflowHistoryRepository;
    private readonly IStripeService _stripeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IClaimRepository claimRepository,
        IPolicyRepository policyRepository,
        IClaimWorkflowHistoryRepository workflowHistoryRepository,
        IStripeService stripeService,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _claimRepository = claimRepository;
        _policyRepository = policyRepository;
        _workflowHistoryRepository = workflowHistoryRepository;
        _stripeService = stripeService;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<(string PaymentIntentId, decimal FinalPayableAmount)>> CreatePaymentIntentAsync(Guid claimId)
    {
        _logger.LogInformation("Creating payment intent for claim {ClaimId}", claimId);
        try
        {
            var validationResult = await ValidateClaimForPaymentAsync(claimId);
            if (validationResult.IsFailure)
            {
                return Result<(string PaymentIntentId, decimal FinalPayableAmount)>.Failure(validationResult.Error);
            }

            var claim = validationResult.Value!;
            var idempotencyKey = Guid.NewGuid();
            var metadata = new Dictionary<string, string>
            {
                { "claimId", claimId.ToString() },
                { "claimNumber", claim.ClaimNumber }
            };

            var paymentIntent = await _stripeService.CreatePaymentIntentAsync(
                claim.FinalPayableAmount,
                "inr",
                idempotencyKey.ToString(),
                metadata);

            var claimPayment = BuildClaimPayment(claim, paymentIntent.Id, idempotencyKey);
            await _paymentRepository.AddAsync(claimPayment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Payment intent created for claim {ClaimId} with amount {Amount}",
                claimId, claim.FinalPayableAmount);

            return Result<(string PaymentIntentId, decimal FinalPayableAmount)>.Success((paymentIntent.Id, claim.FinalPayableAmount));
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation during payment intent creation for claim {ClaimId}", claimId);
            return Result<(string PaymentIntentId, decimal FinalPayableAmount)>.Failure(Error.Validation("ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for claim {ClaimId}", claimId);
            return Result<(string PaymentIntentId, decimal FinalPayableAmount)>.Failure(
                Error.Validation("CreatePaymentIntentFailed", "An error occurred while creating the payment intent."));
        }
    }

    public async Task<Result<(bool Success, decimal FinalPayableAmount)>> ConfirmPaymentAsync(Guid claimId, string paymentIntentId)
    {
        _logger.LogInformation("Confirming payment for claim {ClaimId}", claimId);
        try
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                _logger.LogWarning("Claim {ClaimId} not found during payment confirmation", claimId);
                return Result<(bool Success, decimal FinalPayableAmount)>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
            }

            if (claim.Status == ClaimStatus.Closed)
            {
                _logger.LogWarning("Claim {ClaimId} is already closed", claimId);
                return Result<(bool Success, decimal FinalPayableAmount)>.Failure(
                    Error.Conflict("ClaimAlreadyClosed", "Claim is already closed."));
            }

            var payments = await _paymentRepository.GetByClaimIdAsync(claimId);
            var claimPayment = payments.FirstOrDefault(p => p.StripePaymentIntentId == paymentIntentId);
            if (claimPayment == null)
            {
                _logger.LogWarning("Payment not found for payment intent {PaymentIntentId}", paymentIntentId);
                return Result<(bool Success, decimal FinalPayableAmount)>.Failure(
                    Error.NotFound("PaymentNotFound", "Payment record not found for the specified payment intent."));
            }

            if (claimPayment.PaymentStatus == ClaimPaymentStatus.Completed)
            {
                _logger.LogWarning("Payment already completed for claim {ClaimId}", claimId);
                return Result<(bool Success, decimal FinalPayableAmount)>.Failure(
                    Error.Conflict("PaymentAlreadyCompleted", "Payment has already been completed."));
            }

            var confirmation = await _stripeService.ConfirmPaymentAsync(paymentIntentId);
            if (confirmation.Status.ToLower() != "succeeded")
            {
                claimPayment.PaymentStatus = ClaimPaymentStatus.Failed;
                await _paymentRepository.UpdateAsync(claimPayment);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogWarning("Payment failed for claim {ClaimId}: {FailureMessage}", claimId, confirmation.FailureMessage);
                return Result<(bool Success, decimal FinalPayableAmount)>.Failure(
                    Error.Validation("PaymentFailed", $"Payment failed: {confirmation.FailureMessage ?? "Unknown error"}"));
            }

            await UpdateClaimOnPaymentAsync(claim, claimPayment);
            await _claimRepository.UpdateAsync(claim);
            await _paymentRepository.UpdateAsync(claimPayment);
            await UpdatePolicyCoverageAsync(claim);
            var workflowEntry = BuildPaymentWorkflowEntry(claim);
            await _workflowHistoryRepository.AddAsync(workflowEntry);
            await _unitOfWork.SaveChangesAsync();

            // Send notification about payment confirmation
            var policy = await _policyRepository.GetByIdAsync(claim.PolicyId);
            if (policy != null)
            {
                await _notificationService.CreateNotificationAsync(
                    policy.PolicyHolderId,
                    "Payment Confirmed",
                    $"The payment of {claimPayment.Amount} for claim {claim.ClaimNumber} has been successfully processed and the claim is now closed.",
                    InsuranceClaimSystem.Domain.Enums.NotificationType.StatusChanged,
                    InsuranceClaimSystem.Domain.Enums.NotificationChannel.InApp,
                    claim.Id);
            }

            _logger.LogInformation(
                "Payment confirmed for claim {ClaimId} with amount {Amount}",
                claimId, claimPayment.Amount);

            return Result<(bool Success, decimal FinalPayableAmount)>.Success((true, claim.FinalPayableAmount));
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation during payment confirmation for claim {ClaimId}", claimId);
            return Result<(bool Success, decimal FinalPayableAmount)>.Failure(Error.Validation("ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment for claim {ClaimId}", claimId);
            return Result<(bool Success, decimal FinalPayableAmount)>.Failure(
                Error.Validation("ConfirmPaymentFailed", "An error occurred while confirming the payment."));
        }
    }

    public async Task<Result<ClaimPaymentDto?>> GetPaymentByClaimIdAsync(Guid claimId)
    {
        try
        {
            var payments = await _paymentRepository.GetByClaimIdAsync(claimId);
            var payment = payments.FirstOrDefault();

            if (payment == null)
            {
                return Result<ClaimPaymentDto?>.Success(null);
            }

            var dto = new ClaimPaymentDto
            {
                Id = payment.Id,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentStatus = payment.PaymentStatus,
                ProcessedAt = payment.ProcessedAt
            };

            return Result<ClaimPaymentDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment for claim {ClaimId}", claimId);
            return Result<ClaimPaymentDto?>.Failure(
                Error.Validation("GetPaymentFailed", "An error occurred while retrieving the payment."));
        }
    }

    private async Task<Result<Claim>> ValidateClaimForPaymentAsync(Guid claimId)
    {
        var claim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
        if (claim == null)
        {
            return Result<Claim>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
        }

        if (claim.Status == ClaimStatus.Closed)
        {
            return Result<Claim>.Failure(
                Error.Conflict("ClaimAlreadyClosed", "Claim is already closed."));
        }

        if (claim.Status != ClaimStatus.Approved)
        {
            return Result<Claim>.Failure(
                Error.Validation("ClaimNotApproved", "Payment can only be initiated for approved claims."));
        }

        var existingPayments = await _paymentRepository.GetByClaimIdAsync(claimId);
        var completedPayment = existingPayments.FirstOrDefault(p => p.PaymentStatus == ClaimPaymentStatus.Completed);
        if (completedPayment != null)
        {
            return Result<Claim>.Failure(
                Error.Conflict("PaymentAlreadyCompleted", "Payment for this claim has already been completed."));
        }

        if (claim.FinalPayableAmount <= 0)
        {
            return Result<Claim>.Failure(
                Error.Validation("InvalidAmount", "Final payable amount must be greater than zero."));
        }

        return Result<Claim>.Success(claim);
    }

    private static ClaimPayment BuildClaimPayment(Claim claim, string paymentIntentId, Guid idempotencyKey)
    {
        return new ClaimPayment
        {
            ClaimId = claim.Id,
            Amount = claim.FinalPayableAmount,
            RecipientType = claim.PaymentRecipientType,
            RecipientName = claim.RecipientName,
            RecipientAccountNumber = claim.RecipientAccountNumber,
            RecipientBankName = claim.RecipientBankName,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentStatus = ClaimPaymentStatus.Pending,
            StripePaymentIntentId = paymentIntentId,
            IdempotencyKey = idempotencyKey
        };
    }

    private static async Task UpdateClaimOnPaymentAsync(Claim claim, ClaimPayment claimPayment)
    {
        claimPayment.PaymentStatus = ClaimPaymentStatus.Completed;
        claimPayment.ProcessedAt = DateTime.UtcNow;

        claim.Status = ClaimStatus.Closed;
        claim.ResolvedAt = DateTime.UtcNow;
    }

    private async Task UpdatePolicyCoverageAsync(Claim claim)
    {
        var policy = await _policyRepository.GetByIdAsync(claim.PolicyId);
        if (policy != null)
        {
            policy.RemainingCoverageAmount -= claim.FinalPayableAmount;
            if (policy.RemainingCoverageAmount <= 0)
            {
                policy.RemainingCoverageAmount = 0;
                policy.Status = PolicyStatus.CoverageExhausted;
            }
            await _policyRepository.UpdateAsync(policy);
        }
    }

    private static ClaimWorkflowHistory BuildPaymentWorkflowEntry(Claim claim)
    {
        return new ClaimWorkflowHistory
        {
            ClaimId = claim.Id,
            ChangedByUserId = claim.AssignedManagerId ?? claim.AssignedReviewerId ?? claim.ClaimantId,
            ActionType = WorkflowActionType.StatusChange,
            PreviousStatus = claim.Status,
            NewStatus = ClaimStatus.Closed,
            Comments = $"Payment of {claim.FinalPayableAmount} completed via Stripe"
        };
    }
}