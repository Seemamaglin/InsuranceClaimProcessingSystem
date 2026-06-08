using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Claims;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
    public DateTime UploadedAt { get; set; }
}