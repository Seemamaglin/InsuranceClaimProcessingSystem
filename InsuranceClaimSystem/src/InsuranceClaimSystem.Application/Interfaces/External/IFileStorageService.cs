using Microsoft.AspNetCore.Http;

namespace InsuranceClaimSystem.Application.Interfaces.External;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);
    Task<byte[]> GetFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
    Task<string> SaveClaimFileAsync(IFormFile file, Guid claimId);
    Task<string> SaveKYCFileAsync(IFormFile file, Guid userId);
    Task<(byte[] bytes, string mimeType, string fileName)> GetFileWithMetadataAsync(string filePath);
    Task<string> ConvertToWebPAsync(IFormFile file, Guid claimId);
}
