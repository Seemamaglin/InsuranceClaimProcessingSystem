using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities
{
    public class KYCDocument : BaseEntity
    {
        // Foreign Keys
        public Guid UserId { get; set; }
        public Guid? VerifiedByAdminId { get; set; }

        // Document Info
        public KycDocumentType DocumentType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        // Verification
        public VerificationStatus VerificationStatus { get; set; }
            = VerificationStatus.Pending;              // always starts as Pending
        public string? RejectionReason { get; set; }  // null until Admin rejects
        public DateTime? VerifiedAt { get; set; }      // null until verified or rejected

        public User User { get; set; } = null!;
        public User? VerifiedByAdmin { get; set; }
    }
}