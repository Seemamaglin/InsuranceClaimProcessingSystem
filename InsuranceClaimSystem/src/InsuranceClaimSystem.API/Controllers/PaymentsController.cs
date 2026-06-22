using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InsuranceClaimSystem.Application.Interfaces.Services;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Create a payment intent for an approved claim
    /// </summary>
    [HttpPost("{claimId:guid}/create-intent")]
    [Authorize(Policy = "FinanceOfficerOnly")]
    public async Task<IActionResult> CreatePaymentIntent(Guid claimId)
    {
        _logger.LogInformation("API: {Action} called", nameof(CreatePaymentIntent));
        var result = await _paymentService.CreatePaymentIntentAsync(claimId);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(CreatePaymentIntent), result.Error.Code);
            if (result.Error.Code == "ClaimNotFound")
            {
                return NotFound(result.Error);
            }
            if (result.Error.Code == "ClaimAlreadyClosed" || result.Error.Code == "PaymentAlreadyCompleted")
            {
                return Conflict(result.Error);
            }
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(CreatePaymentIntent));
        return Ok(new { 
            paymentIntentId = result.Value.PaymentIntentId,
            finalPayableAmount = result.Value.FinalPayableAmount
        });
    }

    /// <summary>
    /// Confirm payment for a claim
    /// </summary>
    [HttpPost("{claimId:guid}/confirm")]
    [Authorize(Policy = "FinanceOfficerOnly")]
    public async Task<IActionResult> ConfirmPayment(Guid claimId, [FromBody] ConfirmPaymentRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(ConfirmPayment));
        var result = await _paymentService.ConfirmPaymentAsync(claimId, request.PaymentIntentId);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(ConfirmPayment), result.Error.Code);
            if (result.Error.Code == "ClaimNotFound" || result.Error.Code == "PaymentNotFound")
            {
                return NotFound(result.Error);
            }
            if (result.Error.Code == "PaymentAlreadyCompleted" || result.Error.Code == "ClaimAlreadyClosed")
            {
                return Conflict(result.Error);
            }
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(ConfirmPayment));
        return Ok(new { 
            success = result.Value.Success,
            finalPayableAmount = result.Value.FinalPayableAmount
        });
    }

    /// <summary>
    /// Get payment details by claim ID
    /// </summary>
    [HttpGet("{claimId:guid}")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<IActionResult> GetPaymentByClaimId(Guid claimId)
    {
        _logger.LogInformation("API: {Action} called", nameof(GetPaymentByClaimId));
        var result = await _paymentService.GetPaymentByClaimIdAsync(claimId);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetPaymentByClaimId), result.Error.Code);
            return BadRequest(result.Error);
        }
        if (result.Value == null)
        {
            _logger.LogWarning("API: {Action} failed - PaymentNotFound", nameof(GetPaymentByClaimId));
            return NotFound(new { message = "No payment found for this claim." });
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetPaymentByClaimId));
        return Ok(result.Value);
    }
}

public class ConfirmPaymentRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;
}