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
        var result = await _paymentService.CreatePaymentIntentAsync(claimId);
        if (result.IsFailure)
        {
            if (result.Error.Code == "ClaimNotFound")
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);
        }
        return Ok(new { paymentIntentId = result.Value });
    }

    /// <summary>
    /// Confirm payment for a claim
    /// </summary>
    [HttpPost("{claimId:guid}/confirm")]
    [Authorize(Policy = "FinanceOfficerOnly")]
    public async Task<IActionResult> ConfirmPayment(Guid claimId, [FromBody] ConfirmPaymentRequest request)
    {
        var result = await _paymentService.ConfirmPaymentAsync(claimId, request.PaymentIntentId);
        if (result.IsFailure)
        {
            if (result.Error.Code == "ClaimNotFound" || result.Error.Code == "PaymentNotFound")
            {
                return NotFound(result.Error);
            }
            if (result.Error.Code == "PaymentAlreadyCompleted")
            {
                return Conflict(result.Error);
            }
            return BadRequest(result.Error);
        }
        return Ok(new { success = result.Value });
    }

    /// <summary>
    /// Get payment details by claim ID
    /// </summary>
    [HttpGet("{claimId:guid}")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<IActionResult> GetPaymentByClaimId(Guid claimId)
    {
        var result = await _paymentService.GetPaymentByClaimIdAsync(claimId);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        if (result.Value == null)
        {
            return NotFound(new { message = "No payment found for this claim." });
        }
        return Ok(result.Value);
    }
}

public class ConfirmPaymentRequest
{
    public string PaymentIntentId { get; set; } = string.Empty;
}