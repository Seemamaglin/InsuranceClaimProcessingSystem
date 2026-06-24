using Microsoft.AspNetCore.Http;

namespace InsuranceClaimSystem.Application.Interfaces.External;

// TODO [PRODUCTION DEPLOYMENT]: DPDP Act 2023 Compliance
// KYC documents contain highly sensitive legal data (e.g., Aadhaar).
// Local disk storage is acceptable ONLY for local development.
// Before production, implement an Azure Blob Storage or AWS S3 provider
// with private buckets and at-rest encryption to ensure strict data privacy.
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
