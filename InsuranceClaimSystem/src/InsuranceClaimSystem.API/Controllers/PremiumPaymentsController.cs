using System.Security.Claims;
using InsuranceClaimSystem.Application.DTOs.Policies;
using InsuranceClaimSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/policies")]
public class PremiumPaymentsController : ControllerBase
{
    private readonly IPremiumPaymentService _premiumPaymentService;
    private readonly ILogger<PremiumPaymentsController> _logger;

    public PremiumPaymentsController(IPremiumPaymentService premiumPaymentService, ILogger<PremiumPaymentsController> logger)
    {
        _premiumPaymentService = premiumPaymentService;
        _logger = logger;
    }

    [HttpPost("{policyId:guid}/pay-premium")]
    [Authorize(Policy = "PolicyHolderOnly")]
    public async Task<IActionResult> PayPremium(Guid policyId, [FromBody] PayPremiumRequest request)
    {
        _logger.LogInformation("API: PayPremium called for policy {PolicyId}", policyId);
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var policyHolderId))
        {
            _logger.LogWarning("API: PayPremium failed - InvalidUserId");
            return Unauthorized(new { error = "Invalid user identity." });
        }

        request.PolicyId = policyId;
        var result = await _premiumPaymentService.PayPremiumAsync(policyHolderId, request);

        if (result.IsFailure)
        {
            _logger.LogWarning("API: PayPremium failed - {ErrorCode}", result.Error.Code);
            return result.Error.Code switch
            {
                "PolicyNotFound" => NotFound(result.Error),
                "Unauthorized" => StatusCode(403, result.Error),
                "PolicyNotActive" => BadRequest(result.Error),
                "AmountMismatch" => BadRequest(result.Error),
                _ => Conflict(result.Error)
            };
        }

        _logger.LogInformation("API: PayPremium succeeded for policy {PolicyId}", policyId);
        return Ok(result.Value);
    }
}