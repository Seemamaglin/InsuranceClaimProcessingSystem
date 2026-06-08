using AutoMapper;
using Microsoft.AspNetCore.Http;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IClaimRepository _claimRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentService> _logger;

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const long ImageCompressionThreshold = 500 * 1024; // 500KB
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "image/webp"
    };

    public DocumentService(
        IDocumentRepository documentRepository,
        IFileStorageService fileStorageService,
        IClaimRepository claimRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _claimRepository = claimRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<DocumentDto>> UploadDocumentAsync(Guid claimId, Guid uploadedByUserId, IFormFile file, DocumentType documentType)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return Result<DocumentDto>.Failure(Error.Validation("FileRequired", "File is required."));
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return Result<DocumentDto>.Failure(Error.Validation("FileTooLarge", "File size exceeds the maximum allowed size of 10MB."));
            }

            // Validate MIME type
            if (!AllowedMimeTypes.Contains(file.ContentType))
            {
                return Result<DocumentDto>.Failure(Error.Validation("InvalidFileType", "File type is not allowed."));
            }

            // Verify claim exists
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim == null)
            {
                return Result<DocumentDto>.Failure(Error.NotFound("ClaimNotFound", "Claim not found."));
            }

            // Process file (compression for images if needed)
            var fileName = $"{Guid.NewGuid()}_{SanitizeFileName(file.FileName)}";
            var contentType = file.ContentType;

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // Auto-convert large images to WebP
            if (IsImageFile(contentType) && fileBytes.Length > ImageCompressionThreshold)
            {
                try
                {
                    fileBytes = await ConvertToWebPAsync(fileBytes, contentType);
                    contentType = "image/webp";
                    fileName = Path.ChangeExtension(fileName, ".webp");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to compress image, using original");
                }
            }

            // Save file
            var fileUrl = await _fileStorageService.SaveFileAsync(
                new MemoryStream(fileBytes), 
                fileName, 
                contentType);

            // Create document entity
            var document = new Document
            {
                ClaimId = claimId,
                UploadedByUserId = uploadedByUserId,
                UploadedAt = DateTime.UtcNow,
                FileName = file.FileName,
                FileUrl = fileUrl,
                MimeType = contentType,
                FileSizeInBytes = fileBytes.Length,
                DocumentType = documentType,
                VerificationStatus = VerificationStatus.Pending
            };

            await _documentRepository.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            return Result<DocumentDto>.Success(_mapper.Map<DocumentDto>(document));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for claim {ClaimId}", claimId);
            return Result<DocumentDto>.Failure(Error.Validation("UploadFailed", "An error occurred while uploading the document."));
        }
    }

    public async Task<Result<DocumentDownloadResult>> DownloadDocumentAsync(Guid documentId)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                return Result<DocumentDownloadResult>.Failure(Error.NotFound("DocumentNotFound", "Document not found."));
            }

            var fileContent = await _fileStorageService.GetFileAsync(document.FileUrl);

            return Result<DocumentDownloadResult>.Success(new DocumentDownloadResult
            {
                FileContent = fileContent,
                ContentType = document.MimeType,
                FileName = document.FileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
            return Result<DocumentDownloadResult>.Failure(Error.Validation("DownloadFailed", "An error occurred while downloading the document."));
        }
    }

    public async Task<Result<DocumentDto>> VerifyDocumentAsync(Guid documentId, Guid verifiedByUserId, VerificationStatus status, string? rejectionReason)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                return Result<DocumentDto>.Failure(Error.NotFound("DocumentNotFound", "Document not found."));
            }

            document.VerifiedByUserId = verifiedByUserId;
            document.VerifiedAt = DateTime.UtcNow;
            document.VerificationStatus = status;

            if (status == VerificationStatus.Rejected && !string.IsNullOrEmpty(rejectionReason))
            {
                document.RejectionReason = rejectionReason;
            }

            await _documentRepository.UpdateAsync(document);
            await _unitOfWork.SaveChangesAsync();

            return Result<DocumentDto>.Success(_mapper.Map<DocumentDto>(document));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying document {DocumentId}", documentId);
            return Result<DocumentDto>.Failure(Error.Validation("VerifyFailed", "An error occurred while verifying the document."));
        }
    }

    public async Task<Result<bool>> DeleteDocumentAsync(Guid documentId)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                return Result<bool>.Failure(Error.NotFound("DocumentNotFound", "Document not found."));
            }

            // Delete file from storage
            await _fileStorageService.DeleteFileAsync(document.FileUrl);

            // Soft delete
            await _documentRepository.DeleteAsync(documentId);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return Result<bool>.Failure(Error.Validation("DeleteFailed", "An error occurred while deleting the document."));
        }
    }

    private static bool IsImageFile(string contentType)
    {
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized;
    }

    private Task<byte[]> ConvertToWebPAsync(byte[] originalBytes, string originalMimeType)
    {
        // Note: This requires SixLabors.ImageSharp package
        // For now, return original bytes as fallback
        // In production, implement actual WebP conversion
        return Task.FromResult(originalBytes);
    }
}