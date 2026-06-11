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
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IClaimRepository claimRepository,
        IPolicyRepository policyRepository,
        IClaimWorkflowHistoryRepository workflowHistoryRepository,
        IStripeService stripeService,
        IUnitOfWork unitOfWork,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _claimRepository = claimRepository;
        _policyRepository = policyRepository;
        _workflowHistoryRepository = workflowHistoryRepository;
        _stripeService = stripeService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<string>> CreatePaymentIntentAsync(Guid claimId)
    {
        try
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
            if (claim == null)
            {
                return Result<string>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
            }

            // Verify claim is approved
            if (claim.Status != ClaimStatus.Approved)
            {
                return Result<string>.Failure(
                    Error.Validation("ClaimNotApproved", "Payment can only be initiated for approved claims."));
            }

            // Check if payment already exists and is completed
            var existingPayments = await _paymentRepository.GetByClaimIdAsync(claimId);
            var completedPayment = existingPayments.FirstOrDefault(p => p.PaymentStatus == ClaimPaymentStatus.Completed);
            if (completedPayment != null)
            {
                return Result<string>.Failure(
                    Error.Conflict("PaymentAlreadyCompleted", "Payment for this claim has already been completed."));
            }

            var amount = claim.FinalPayableAmount;
            if (amount <= 0)
            {
                return Result<string>.Failure(
                    Error.Validation("InvalidAmount", "Final payable amount must be greater than zero."));
            }

            var idempotencyKey = Guid.NewGuid();
            var metadata = new Dictionary<string, string>
            {
                { "claimId", claimId.ToString() },
                { "claimNumber", claim.ClaimNumber }
            };

            var paymentIntent = await _stripeService.CreatePaymentIntentAsync(
                amount,
                "inr",
                idempotencyKey.ToString(),
                metadata);

            var claimPayment = new ClaimPayment
            {
                ClaimId = claimId,
                Amount = amount,
                RecipientType = claim.PaymentRecipientType,
                RecipientName = claim.RecipientName,
                RecipientAccountNumber = claim.RecipientAccountNumber,
                RecipientBankName = claim.RecipientBankName,
                PaymentMethod = PaymentMethod.BankTransfer,
                PaymentStatus = ClaimPaymentStatus.Pending,
                StripePaymentIntentId = paymentIntent.Id,
                IdempotencyKey = idempotencyKey
            };

            await _paymentRepository.AddAsync(claimPayment);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Payment intent created for claim {ClaimId} with amount {Amount}",
                claimId, amount);

            return Result<string>.Success(paymentIntent.Id);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation during payment intent creation for claim {ClaimId}", claimId);
            return Result<string>.Failure(Error.Validation("ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for claim {ClaimId}", claimId);
            return Result<string>.Failure(
                Error.Validation("CreatePaymentIntentFailed", "An error occurred while creating the payment intent."));
        }
    }

    public async Task<Result<bool>> ConfirmPaymentAsync(Guid claimId, string paymentIntentId)
    {
        try
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                return Result<bool>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
            }

            var payments = await _paymentRepository.GetByClaimIdAsync(claimId);
            var claimPayment = payments.FirstOrDefault(p => p.StripePaymentIntentId == paymentIntentId);
            if (claimPayment == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound("PaymentNotFound", "Payment record not found for the specified payment intent."));
            }

            if (claimPayment.PaymentStatus == ClaimPaymentStatus.Completed)
            {
                return Result<bool>.Failure(
                    Error.Conflict("PaymentAlreadyCompleted", "Payment has already been completed."));
            }

            // Verify payment status with Stripe
            var confirmation = await _stripeService.ConfirmPaymentAsync(paymentIntentId);

            if (confirmation.Status.ToLower() != "succeeded")
            {
                claimPayment.PaymentStatus = ClaimPaymentStatus.Failed;
                await _paymentRepository.UpdateAsync(claimPayment);
                await _unitOfWork.SaveChangesAsync();

                return Result<bool>.Failure(
                    Error.Validation("PaymentFailed", $"Payment failed: {confirmation.FailureMessage ?? "Unknown error"}"));
            }

            // Update claim payment status
            claimPayment.PaymentStatus = ClaimPaymentStatus.Completed;
            claimPayment.ProcessedAt = DateTime.UtcNow;

            // Update claim status to Closed
            var previousStatus = claim.Status;
            claim.Status = ClaimStatus.Closed;
            claim.ResolvedAt = DateTime.UtcNow;

            // Decrement policy remaining coverage
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

            // Create workflow history entry
            var workflowEntry = new ClaimWorkflowHistory
            {
                ClaimId = claim.Id,
                ChangedByUserId = claim.AssignedManagerId ?? Guid.Empty,
                ActionType = WorkflowActionType.StatusChange,
                PreviousStatus = previousStatus,
                NewStatus = ClaimStatus.Closed,
                Comments = $"Payment of {claim.FinalPayableAmount} completed via Stripe"
            };
            await _workflowHistoryRepository.AddAsync(workflowEntry);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Payment confirmed for claim {ClaimId} with amount {Amount}",
                claimId, claimPayment.Amount);

            return Result<bool>.Success(true);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation during payment confirmation for claim {ClaimId}", claimId);
            return Result<bool>.Failure(Error.Validation("ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment for claim {ClaimId}", claimId);
            return Result<bool>.Failure(
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
}