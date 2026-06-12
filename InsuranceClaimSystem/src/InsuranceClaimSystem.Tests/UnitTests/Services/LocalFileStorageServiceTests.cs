using FluentAssertions;
using InsuranceClaimSystem.Infrastructure.Services.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.WebRootPath).Returns(_tempDir);
        _service = new LocalFileStorageService(_envMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private static IFormFile CreateMockFormFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public async Task SaveClaimFileAsync_WithValidFile_ShouldSaveAndReturnPath()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var content = "Test claim file content"u8.ToArray();
        var file = CreateMockFormFile("claim.pdf", "application/pdf", content);

        // Act
        var result = await _service.SaveClaimFileAsync(file, claimId);

        // Assert
        result.Should().StartWith($"uploads/claims/{claimId}/");
        result.Should().EndWith("_claim.pdf");
        
        var fullPath = Path.Combine(_tempDir, result);
        File.Exists(fullPath).Should().BeTrue();
        var savedContent = await File.ReadAllBytesAsync(fullPath);
        savedContent.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task SaveKYCFileAsync_WithValidFile_ShouldSaveAndReturnPath()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var content = "Test KYC file content"u8.ToArray();
        var file = CreateMockFormFile("kyc.png", "image/png", content);

        // Act
        var result = await _service.SaveKYCFileAsync(file, userId);

        // Assert
        result.Should().StartWith($"uploads/kyc/{userId}/");
        result.Should().EndWith("_kyc.png");
        
        var fullPath = Path.Combine(_tempDir, result);
        File.Exists(fullPath).Should().BeTrue();
        var savedContent = await File.ReadAllBytesAsync(fullPath);
        savedContent.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task GetFileWithMetadataAsync_WithExistingFile_ShouldReturnBytesMimeTypeAndName()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var content = "Test file content"u8.ToArray();
        var file = CreateMockFormFile("document.pdf", "application/pdf", content);
        
        var savedPath = await _service.SaveClaimFileAsync(file, claimId);

        // Act
        var (bytes, mimeType, fileName) = await _service.GetFileWithMetadataAsync(savedPath);

        // Assert
        bytes.Should().BeEquivalentTo(content);
        mimeType.Should().Be("application/pdf");
        fileName.Should().EndWith("_document.pdf");
    }

    [Fact]
    public async Task GetFileAsync_WithExistingFile_ShouldReturnBytes()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var content = "Test file bytes"u8.ToArray();
        var file = CreateMockFormFile("test.txt", "text/plain", content);
        
        var savedPath = await _service.SaveClaimFileAsync(file, claimId);

        // Act
        var result = await _service.GetFileAsync(savedPath);

        // Assert
        result.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_ShouldDeleteFile()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var content = "File to delete"u8.ToArray();
        var file = CreateMockFormFile("delete.txt", "text/plain", content);
        
        var savedPath = await _service.SaveClaimFileAsync(file, claimId);
        var fullPath = Path.Combine(_tempDir, savedPath);
        File.Exists(fullPath).Should().BeTrue();

        // Act
        await _service.DeleteFileAsync(savedPath);

        // Assert
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistingFile_ShouldNotThrow()
    {
        // Arrange
        var nonExistentPath = "uploads/claims/nonexistent/file.txt";

        // Act & Assert
        var act = async () => await _service.DeleteFileAsync(nonExistentPath);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetFileWithMetadataAsync_WithPdf_ShouldReturnCorrectMimeType()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var content = "PDF content"u8.ToArray();
        var file = CreateMockFormFile("report.pdf", "application/pdf", content);
        
        var savedPath = await _service.SaveClaimFileAsync(file, claimId);

        // Act
        var (_, mimeType, _) = await _service.GetFileWithMetadataAsync(savedPath);

        // Assert
        mimeType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task GetFileWithMetadataAsync_WithPng_ShouldReturnCorrectMimeType()
    {
        // Arrange
        var claimId = Guid.NewGuid();
        var content = "PNG content"u8.ToArray();
        var file = CreateMockFormFile("image.png", "image/png", content);
        
        var savedPath = await _service.SaveClaimFileAsync(file, claimId);

        // Act
        var (_, mimeType, _) = await _service.GetFileWithMetadataAsync(savedPath);

        // Assert
        mimeType.Should().Be("image/png");
    }
}