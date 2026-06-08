namespace InsuranceClaimSystem.Infrastructure.Configuration;

public class FileStorageSettings
{
    public string UploadPath { get; set; } = "uploads";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
}