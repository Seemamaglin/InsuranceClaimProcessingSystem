using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface IDocumentService
{
    Task<Result<DocumentDto>> UploadDocumentAsync(Guid claimId, Guid uploadedByUserId, IFormFile file, DocumentType documentType);
    Task<Result<DocumentDownloadResult>> DownloadDocumentAsync(Guid documentId, Guid userId, bool isStaff);
    Task<Result<DocumentDto>> VerifyDocumentAsync(Guid documentId, Guid verifiedByUserId, VerificationStatus status, string? rejectionReason);
    Task<Result<bool>> DeleteDocumentAsync(Guid documentId);
}

public class DocumentDownloadResult
{
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}