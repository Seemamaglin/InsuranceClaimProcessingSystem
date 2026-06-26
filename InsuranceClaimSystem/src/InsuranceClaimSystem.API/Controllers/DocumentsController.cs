using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Enums;
using System.Security.Claims;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a document for a claim
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Policy = "PolicyHolderOnly")]
    public async Task<IActionResult> UploadDocument(
        [FromForm] Guid claimId,
        [FromForm] DocumentType documentType,
        IFormFile file)
    {
        _logger.LogInformation("API: {Action} called", nameof(UploadDocument));
        
        var uploadedByUserIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(uploadedByUserIdStr, out var uploadedByUserId))
        {
            return Unauthorized();
        }
        var result = await _documentService.UploadDocumentAsync(claimId, uploadedByUserId, file, documentType);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(UploadDocument), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(UploadDocument));
        return CreatedAtAction(nameof(DownloadDocument), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Download a document
    /// </summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id)
    {
        _logger.LogInformation("API: {Action} called", nameof(DownloadDocument));
        
        var userIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();
            
        var isStaff = User.IsInRole("Admin") || User.IsInRole("FinanceOfficer") || User.IsInRole("ClaimReviewer") || User.IsInRole("CustomerSupport");

        var result = await _documentService.DownloadDocumentAsync(id, userId, isStaff);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(DownloadDocument), result.Error.Code);
            if (result.Error.Code == "Unauthorized")
                return Forbid();
            return NotFound(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(DownloadDocument));
        return File(result.Value.FileContent, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>
    /// Verify a document (reviewer only)
    /// </summary>
    [HttpPost("{id:guid}/verify")]
    [Authorize(Policy = "ClaimReviewerOnly")]
    public async Task<IActionResult> VerifyDocument(
        Guid id,
        [FromQuery] Guid verifiedByUserId,
        [FromQuery] VerificationStatus status,
        [FromBody] VerifyDocumentRequest? request = null)
    {
        _logger.LogInformation("API: {Action} called", nameof(VerifyDocument));
        var result = await _documentService.VerifyDocumentAsync(id, verifiedByUserId, status, request?.RejectionReason);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(VerifyDocument), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(VerifyDocument));
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a document
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PolicyHolderOnly")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        _logger.LogInformation("API: {Action} called", nameof(DeleteDocument));
        var result = await _documentService.DeleteDocumentAsync(id);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(DeleteDocument), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(DeleteDocument));
        return NoContent();
    }
}

public class VerifyDocumentRequest
{
    public string? RejectionReason { get; set; }
}