using AutoMapper;
using FluentAssertions;
using InsuranceClaimSystem.Application.DTOs.Claims;
using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IClaimRepository> _claimRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<DocumentService>> _loggerMock;
    private readonly DocumentService _documentService;

    public DocumentServiceTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _claimRepositoryMock = new Mock<IClaimRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<DocumentService>>();

        _documentService = new DocumentService(
            _documentRepositoryMock.Object,
            _fileStorageServiceMock.Object,
            _claimRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static IFormFile CreateMockFormFile(string fileName, string contentType, long length)
    {
        var stream = new MemoryStream(new byte[length]);
        return new FormFile(stream, 0, length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task UploadDocumentAsync_WithValidFile_ShouldUploadSuccessfully()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var claim = new Claim { Id = claimId, Status = ClaimStatus.Submitted };
        var file = CreateMockFormFile("test.pdf", "application/pdf", 1024 * 100);

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync(claim);
        _fileStorageServiceMock.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://storage.example.com/documents/test.pdf");
        _documentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Document>())).ReturnsAsync((Document d) => d);
        _mapperMock.Setup(x => x.Map<DocumentDto>(It.IsAny<Document>())).Returns(new DocumentDto { Id = Guid.NewGuid(), FileName = "test.pdf" });

        // Act
        var result = await _documentService.UploadDocumentAsync(claimId, userId, file, DocumentType.MedicalReport);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _documentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Document>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_WithNullFile_ShouldReturnValidationError()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        IFormFile? file = null;

        // Act
        var result = await _documentService.UploadDocumentAsync(claimId, userId, file!, DocumentType.MedicalReport);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("FileRequired");
    }

    [Fact]
    public async Task UploadDocumentAsync_WithOversizedFile_ShouldReturnValidationError()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var file = CreateMockFormFile("large.pdf", "application/pdf", 11 * 1024 * 1024);

        // Act
        var result = await _documentService.UploadDocumentAsync(claimId, userId, file, DocumentType.MedicalReport);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("FileTooLarge");
    }

    [Fact]
    public async Task UploadDocumentAsync_WithInvalidMimeType_ShouldReturnValidationError()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var file = CreateMockFormFile("malicious.exe", "application/x-executable", 1024 * 100);

        // Act
        var result = await _documentService.UploadDocumentAsync(claimId, userId, file, DocumentType.MedicalReport);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidFileType");
    }

    [Fact]
    public async Task UploadDocumentAsync_WithNonExistingClaim_ShouldReturnNotFound()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var file = CreateMockFormFile("test.pdf", "application/pdf", 1024 * 100);

        _claimRepositoryMock.Setup(x => x.GetByIdAsync(claimId)).ReturnsAsync((Claim?)null);

        // Act
        var result = await _documentService.UploadDocumentAsync(claimId, userId, file, DocumentType.MedicalReport);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ClaimNotFound");
    }

    [Fact]
    public async Task DownloadDocumentAsync_WithExistingDocument_ShouldReturnContent()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = new Document
        {
            Id = documentId,
            FileName = "test.pdf",
            FileUrl = "https://storage.example.com/documents/test.pdf",
            MimeType = "application/pdf"
        };
        var fileContent = new byte[] { 1, 2, 3, 4, 5 };

        _documentRepositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync(document);
        _fileStorageServiceMock.Setup(x => x.GetFileAsync(document.FileUrl)).ReturnsAsync(fileContent);

        // Act
        var result = await _documentService.DownloadDocumentAsync(documentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FileContent.Should().BeEquivalentTo(fileContent);
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.FileName.Should().Be("test.pdf");
    }

    [Fact]
    public async Task DownloadDocumentAsync_WithNonExistingDocument_ShouldReturnNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentRepositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync((Document?)null);

        // Act
        var result = await _documentService.DownloadDocumentAsync(documentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DocumentNotFound");
    }

    [Fact]
    public async Task VerifyDocumentAsync_WithValidDocument_ShouldVerify()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var verifierId = Guid.NewGuid();
        var document = new Document
        {
            Id = documentId,
            VerificationStatus = VerificationStatus.Pending
        };

        _documentRepositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync(document);
        _documentRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Document>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<DocumentDto>(It.IsAny<Document>())).Returns(new DocumentDto { Id = documentId, VerificationStatus = VerificationStatus.Verified });

        // Act
        var result = await _documentService.VerifyDocumentAsync(documentId, verifierId, VerificationStatus.Verified, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        document.VerificationStatus.Should().Be(VerificationStatus.Verified);
        document.VerifiedByUserId.Should().Be(verifierId);
        document.VerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyDocumentAsync_WithRejection_ShouldSetRejectionReason()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var verifierId = Guid.NewGuid();
        var rejectionReason = "Document is blurry and illegible";
        var document = new Document
        {
            Id = documentId,
            VerificationStatus = VerificationStatus.Pending
        };

        _documentRepositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync(document);
        _documentRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Document>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<DocumentDto>(It.IsAny<Document>())).Returns(new DocumentDto { Id = documentId, VerificationStatus = VerificationStatus.Rejected });

        // Act
        var result = await _documentService.VerifyDocumentAsync(documentId, verifierId, VerificationStatus.Rejected, rejectionReason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        document.VerificationStatus.Should().Be(VerificationStatus.Rejected);
        document.RejectionReason.Should().Be(rejectionReason);
    }

    [Fact]
    public async Task VerifyDocumentAsync_WithNonExistingDocument_ShouldReturnNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var verifierId = Guid.NewGuid();
        _documentRepositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync((Document?)null);

        // Act
        var result = await _documentService.VerifyDocumentAsync(documentId, verifierId, VerificationStatus.Verified, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DocumentNotFound");
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithExistingDocument_ShouldDelete()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = new Document
        {
            Id = documentId,
            FileUrl = "https://storage.example.com/documents/test.pdf"
        };

        _documentRepositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync(document);
        _fileStorageServiceMock.Setup(x => x.DeleteFileAsync(document.FileUrl)).Returns(Task.CompletedTask);
        _documentRepositoryMock.Setup(x => x.DeleteAsync(documentId)).Returns(Task.CompletedTask);

        // Act
        var result = await _documentService.DeleteDocumentAsync(documentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _fileStorageServiceMock.Verify(x => x.DeleteFileAsync(document.FileUrl), Times.Once);
        _documentRepositoryMock.Verify(x => x.DeleteAsync(documentId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WithNonExistingDocument_ShouldReturnNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentRepositoryMock.Setup(x => x.GetByIdAsync(documentId)).ReturnsAsync((Document?)null);

        // Act
        var result = await _documentService.DeleteDocumentAsync(documentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DocumentNotFound");
    }
}