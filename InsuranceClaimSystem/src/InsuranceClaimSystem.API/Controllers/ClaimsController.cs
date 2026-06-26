using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.Services;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(IClaimService claimService, ILogger<ClaimsController> logger)
    {
        _claimService = claimService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new claim
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "PolicyHolderOnly")]
    public async Task<IActionResult> SubmitClaim([FromBody] SubmitClaimRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(SubmitClaim));
        var result = await _claimService.SubmitClaimAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(SubmitClaim), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(SubmitClaim));
        return CreatedAtAction(nameof(GetClaimById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Save a claim as a draft (skips complex validations)
    /// </summary>
    [HttpPost("draft")]
    [Authorize(Policy = "PolicyHolderOnly")]
    public async Task<IActionResult> SaveDraft([FromBody] SaveClaimDraftRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(SaveDraft));
        var result = await _claimService.SaveAsDraftAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(SaveDraft), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(SaveDraft));
        return Ok(result.Value);
    }

    /// <summary>
    /// Get paginated list of all claims (staff only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "StaffOnly")]
    public async Task<IActionResult> GetClaims([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] InsuranceClaimSystem.Domain.Enums.ClaimStatus? status = null, [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        _logger.LogInformation("API: {Action} called with status {Status} dateFrom {DateFrom} dateTo {DateTo}", nameof(GetClaims), status, dateFrom, dateTo);
        var result = await _claimService.GetClaimsAsync(page, pageSize, status, dateFrom, dateTo);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetClaims), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetClaims));
        return Ok(result.Value);
    }

    /// <summary>
    /// Get claim by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetClaimById(Guid id)
    {
        _logger.LogInformation("API: {Action} called", nameof(GetClaimById));
        var result = await _claimService.GetClaimByIdAsync(id);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetClaimById), result.Error.Code);
            return NotFound(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetClaimById));
        return Ok(result.Value);
    }

    /// <summary>
    /// Get claim by claim number
    /// </summary>
    [HttpGet("number/{claimNumber}")]
    public async Task<IActionResult> GetClaimByNumber(string claimNumber)
    {
        _logger.LogInformation("API: {Action} called", nameof(GetClaimByNumber));
        var result = await _claimService.GetClaimByNumberAsync(claimNumber);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetClaimByNumber), result.Error.Code);
            return NotFound(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetClaimByNumber));
        return Ok(result.Value);
    }

    /// <summary>
    /// Update claim status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "ReviewerOrManager")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateClaimStatusRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(UpdateStatus));
        var result = await _claimService.UpdateStatusAsync(id, request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(UpdateStatus), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(UpdateStatus));
        return Ok(result.Value);
    }

    /// <summary>
    /// Assign a reviewer to a claim
    /// </summary>
    [HttpPost("{id:guid}/assign-reviewer")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> AssignReviewer(Guid id, [FromBody] AssignReviewerRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(AssignReviewer));
        request.ClaimId = id;
        var result = await _claimService.AssignReviewerAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(AssignReviewer), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(AssignReviewer));
        return Ok(result.Value);
    }

    /// <summary>
    /// Auto-assign a reviewer to a claim
    /// </summary>
    [HttpPost("{id:guid}/auto-assign")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> AutoAssignReviewer(Guid id)
    {
        _logger.LogInformation("API: {Action} called", nameof(AutoAssignReviewer));
        var result = await _claimService.AutoAssignReviewerAsync(id);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(AutoAssignReviewer), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(AutoAssignReviewer));
        return Ok(result.Value);
    }

    /// <summary>
    /// Approve a claim (manager only)
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "ClaimsManagerOnly")]
    public async Task<IActionResult> ApproveClaim(Guid id, [FromQuery] Guid managerId, [FromQuery] decimal? approvedAmount = null)
    {
        _logger.LogInformation("API: {Action} called", nameof(ApproveClaim));
        // Note: This would call a dedicated ApproveClaimAsync method
        // For now, returning not implemented
        _logger.LogInformation("API: {Action} succeeded (not implemented)", nameof(ApproveClaim));
        return StatusCode(501, "Claim approval endpoint - use UpdateStatus with Approved status");
    }

    /// <summary>
    /// Reject a claim (manager only)
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "ClaimsManagerOnly")]
    public async Task<IActionResult> RejectClaim(Guid id, [FromQuery] Guid managerId, [FromBody] RejectClaimRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(RejectClaim));
        // Note: This would call a dedicated RejectClaimAsync method
        // For now, returning not implemented
        _logger.LogInformation("API: {Action} succeeded (not implemented)", nameof(RejectClaim));
        return StatusCode(501, "Claim rejection endpoint - use UpdateStatus with Rejected status");
    }

    /// <summary>
    /// Get claims by policy ID
    /// </summary>
    [HttpGet("policy/{policyId:guid}")]
    public async Task<IActionResult> GetClaimsByPolicy(Guid policyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("API: {Action} called", nameof(GetClaimsByPolicy));
        var result = await _claimService.GetClaimsByPolicyAsync(policyId, page, pageSize);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetClaimsByPolicy), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetClaimsByPolicy));
        return Ok(result.Value);
    }

    /// <summary>
    /// Get claims assigned to a reviewer
    /// </summary>
    [HttpGet("reviewer/{reviewerId:guid}")]
    [Authorize(Policy = "ClaimReviewerOnly")]
    public async Task<IActionResult> GetClaimsByReviewer(Guid reviewerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("API: {Action} called", nameof(GetClaimsByReviewer));
        var result = await _claimService.GetClaimsByReviewerAsync(reviewerId, page, pageSize);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetClaimsByReviewer), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetClaimsByReviewer));
        return Ok(result.Value);
    }
}

public class RejectClaimRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}