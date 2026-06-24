using InsuranceClaimSystem.Application.Interfaces.External;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using InsuranceClaimSystem.Infrastructure.Configuration;

namespace InsuranceClaimSystem.Infrastructure.Services.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _wwwroot;
    private readonly FileStorageSettings _settings;
    private readonly ILogger<LocalFileStorageService> _logger;

    // TODO [PRODUCTION DEPLOYMENT]: DPDP Act 2023 Compliance
    // KYC documents contain highly sensitive legal data (e.g., Aadhaar).
    // Local disk storage is acceptable ONLY for local development.
    // Before production, implement an Azure Blob Storage or AWS S3 provider
    // with private buckets and at-rest encryption to ensure strict data privacy.
    
    public LocalFileStorageService(
        IWebHostEnvironment webHostEnvironment,
        IOptions<FileStorageSettings> options,
        ILogger<LocalFileStorageService> logger)
    {
        _wwwroot = webHostEnvironment.WebRootPath;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<string> SaveClaimFileAsync(IFormFile file, Guid claimId)
    {
        var dir = Path.Combine(_wwwroot, "uploads", "claims", claimId.ToString());
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(dir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"uploads/claims/{claimId}/{fileName}";
    }

    public async Task<string> SaveKYCFileAsync(IFormFile file, Guid userId)
    {
        var dir = Path.Combine(_wwwroot, "uploads", "kyc", userId.ToString());
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(dir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"uploads/kyc/{userId}/{fileName}";
    }

    public async Task<(byte[] bytes, string mimeType, string fileName)> GetFileWithMetadataAsync(string filePath)
    {
        var fullPath = Path.Combine(_wwwroot, filePath);
        var bytes = await File.ReadAllBytesAsync(fullPath);
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var mimeType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
        return (bytes, mimeType, Path.GetFileName(filePath));
    }

    public Task DeleteFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_wwwroot, filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public async Task<string> ConvertToWebPAsync(IFormFile file, Guid claimId)
    {
        var dir = Path.Combine(_wwwroot, "uploads", "claims", claimId.ToString());
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}.webp";
        var filePath = Path.Combine(dir, fileName);

        using var image = await Image.LoadAsync(file.OpenReadStream());
        await image.SaveAsWebpAsync(filePath);

        return $"uploads/claims/{claimId}/{fileName}";
    }

    public Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
        => throw new NotImplementedException("Use SaveClaimFileAsync or SaveKYCFileAsync instead.");

    public Task<byte[]> GetFileAsync(string filePath)
        => File.ReadAllBytesAsync(Path.Combine(_wwwroot, filePath));
}
