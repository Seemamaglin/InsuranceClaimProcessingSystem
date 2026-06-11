using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.Services;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/reviewers")]
[Authorize(Policy = "ClaimReviewerOnly")]
public class ReviewersController : ControllerBase
{
    private readonly IClaimService _claimService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<ReviewersController> _logger;

    public ReviewersController(
        IClaimService claimService,
        IDocumentService documentService,
        ILogger<ReviewersController> logger)
    {
        _claimService = claimService;
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Get claims assigned to the current reviewer
    /// </summary>
    [HttpGet("queue")]
    public async Task<IActionResult> GetAssignedClaims(
        [FromQuery] Guid reviewerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _claimService.GetClaimsByReviewerAsync(reviewerId, page, pageSize);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Request additional documents for a claim
    /// </summary>
    [HttpPost("claims/{id:guid}/request-documents")]
    public async Task<IActionResult> RequestDocuments(Guid id, [FromBody] RequestDocumentsRequest request)
    {
        // This would typically create a notification to the policy holder
        // and optionally update claim status to DocumentsPending
        _logger.LogInformation("Document request for claim {ClaimId}: {Message}", id, request.Message);
        
        // Update claim status to documents pending if needed
        var statusUpdate = new UpdateClaimStatusRequest
        {
            NewStatus = Domain.Enums.ClaimStatus.DocumentsPending,
            ChangedByUserId = request.ReviewerId,
            RejectionReason = null
        };
        
        var updateResult = await _claimService.UpdateStatusAsync(id, statusUpdate);
        if (updateResult.IsFailure)
        {
            return BadRequest(updateResult.Error);
        }

        return Ok(new { message = "Document request sent successfully" });
    }

    /// <summary>
    /// Verify all documents for a claim
    /// </summary>
    [HttpPost("claims/{id:guid}/verify-documents")]
    public async Task<IActionResult> VerifyDocuments(Guid id, [FromBody] VerifyDocumentsRequest request)
    {
        // Verify each document
        foreach (var docVerification in request.DocumentVerifications)
        {
            var result = await _documentService.VerifyDocumentAsync(
                docVerification.DocumentId,
                request.ReviewerId,
                docVerification.Status,
                docVerification.RejectionReason);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to verify document {DocumentId}: {Error}", 
                    docVerification.DocumentId, result.Error);
            }
        }

        // If all documents are verified, move claim to UnderReview
        var statusUpdate = new UpdateClaimStatusRequest
        {
            NewStatus = Domain.Enums.ClaimStatus.UnderReview,
            ChangedByUserId = request.ReviewerId,
            RejectionReason = null
        };

        var updateResult = await _claimService.UpdateStatusAsync(id, statusUpdate);
        if (updateResult.IsFailure)
        {
            return BadRequest(updateResult.Error);
        }

        return Ok(new { message = "Documents verified and claim updated" });
    }
}

public class RequestDocumentsRequest
{
    public Guid ReviewerId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class VerifyDocumentsRequest
{
    public Guid ReviewerId { get; set; }
    public List<DocumentVerification> DocumentVerifications { get; set; } = new();
}

public class DocumentVerification
{
    public Guid DocumentId { get; set; }
    public Domain.Enums.VerificationStatus Status { get; set; }
    public string? RejectionReason { get; set; }
}