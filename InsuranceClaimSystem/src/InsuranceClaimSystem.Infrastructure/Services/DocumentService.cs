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

    public async Task<Result<DocumentDto>> UploadDocumentAsync(
        Guid claimId, 
        Guid uploadedByUserId, 
        IFormFile file, 
        DocumentType documentType)
    {
        _logger.LogInformation("Uploading document for claim {ClaimId}", claimId);
        try
        {
            var validationResult = await ValidateFileAsync(claimId, file);
            if (validationResult.IsFailure)
            {
                _logger.LogWarning("File validation failed for claim {ClaimId}: {Error}", claimId, validationResult.Error.Description);
                return Result<DocumentDto>.Failure(validationResult.Error);
            }

            var claim = validationResult.Value!;
            var (fileBytes, contentType, fileName) = await ProcessFileAsync(file);

            var fileUrl = await _fileStorageService.SaveClaimFileAsync(file, claimId);

            var document = BuildDocumentEntity(claimId, uploadedByUserId, file, fileUrl, fileBytes, contentType, documentType);

            await _documentRepository.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Document uploaded successfully for claim {ClaimId}", claimId);
            return Result<DocumentDto>.Success(_mapper.Map<DocumentDto>(document));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for claim {ClaimId}", claimId);
            return Result<DocumentDto>.Failure(
                Error.Validation("UploadFailed", "An error occurred while uploading the document."));
        }
    }

    public async Task<Result<DocumentDownloadResult>> DownloadDocumentAsync(Guid documentId)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                return Result<DocumentDownloadResult>.Failure(
                    Error.NotFound("DocumentNotFound", "Document not found."));
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
            return Result<DocumentDownloadResult>.Failure(
                Error.Validation("DownloadFailed", "An error occurred while downloading the document."));
        }
    }

    public async Task<Result<DocumentDto>> VerifyDocumentAsync(
        Guid documentId, 
        Guid verifiedByUserId, 
        VerificationStatus status, 
        string? rejectionReason)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                return Result<DocumentDto>.Failure(
                    Error.NotFound("DocumentNotFound", "Document not found."));
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
            return Result<DocumentDto>.Failure(
                Error.Validation("VerifyFailed", "An error occurred while verifying the document."));
        }
    }

    public async Task<Result<bool>> DeleteDocumentAsync(Guid documentId)
    {
        try
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound("DocumentNotFound", "Document not found."));
            }

            await _fileStorageService.DeleteFileAsync(document.FileUrl);

            await _documentRepository.DeleteAsync(documentId);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return Result<bool>.Failure(
                Error.Validation("DeleteFailed", "An error occurred while deleting the document."));
        }
    }

    private async Task<Result<Claim>> ValidateFileAsync(Guid claimId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Result<Claim>.Failure(
                Error.Validation("FileRequired", "File is required."));
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return Result<Claim>.Failure(
                Error.Validation("FileTooLarge", "File size exceeds the maximum allowed size of 10MB."));
        }

        if (!AllowedMimeTypes.Contains(file.ContentType))
        {
            return Result<Claim>.Failure(
                Error.Validation("InvalidFileType", "File type is not allowed."));
        }

        var claim = await _claimRepository.GetByIdAsync(claimId);
        if (claim == null)
        {
            return Result<Claim>.Failure(
                Error.NotFound("ClaimNotFound", "Claim not found."));
        }

        return Result<Claim>.Success(claim);
    }

    private async Task<(byte[] fileBytes, string contentType, string fileName)> ProcessFileAsync(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        var contentType = file.ContentType;
        var fileName = $"{Guid.NewGuid()}_{SanitizeFileName(file.FileName)}";

        if (IsImageFile(contentType) && fileBytes.Length > ImageCompressionThreshold)
        {
            var (compressedBytes, newContentType, newFileName) = await CompressImageIfNeededAsync(fileBytes, contentType, fileName);
            fileBytes = compressedBytes;
            contentType = newContentType;
            fileName = newFileName;
        }

        return (fileBytes, contentType, fileName);
    }

    private static Document BuildDocumentEntity(
        Guid claimId, 
        Guid uploadedByUserId, 
        IFormFile file, 
        string fileUrl, 
        byte[] fileBytes, 
        string contentType, 
        DocumentType documentType)
    {
        return new Document
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
    }

    private async Task<(byte[] fileBytes, string contentType, string fileName)> CompressImageIfNeededAsync(
        byte[] fileBytes, 
        string contentType, 
        string fileName)
    {
        try
        {
            var compressed = await ConvertToWebPAsync(fileBytes, contentType);
            return (compressed, "image/webp", Path.ChangeExtension(fileName, ".webp"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to compress image, using original");
            return (fileBytes, contentType, fileName);
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
        return Task.FromResult(originalBytes);
    }
}