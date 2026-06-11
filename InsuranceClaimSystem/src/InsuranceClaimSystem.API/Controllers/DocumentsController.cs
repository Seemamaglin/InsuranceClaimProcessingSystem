using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Enums;

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
        [FromQuery] Guid claimId,
        [FromQuery] Guid uploadedByUserId,
        [FromQuery] DocumentType documentType,
        IFormFile file)
    {
        var result = await _documentService.UploadDocumentAsync(claimId, uploadedByUserId, file, documentType);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return CreatedAtAction(nameof(DownloadDocument), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Download a document
    /// </summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id)
    {
        var result = await _documentService.DownloadDocumentAsync(id);
        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }
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
        var result = await _documentService.VerifyDocumentAsync(id, verifiedByUserId, status, request?.RejectionReason);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a document
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PolicyHolderOnly")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var result = await _documentService.DeleteDocumentAsync(id);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return NoContent();
    }
}

public class VerifyDocumentRequest
{
    public string? RejectionReason { get; set; }
}