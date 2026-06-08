using System;
using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities
{
    // Inherits: Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt from BaseEntity
    public class Document : BaseEntity
    {
        public Guid ClaimId { get; set; }
        public Guid UploadedByUserId { get; set; }
        public DateTime UploadedAt {get; set; }
        
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;   //type of document uploaded like pdf, image, video
        public long FileSizeInBytes { get; set; }
        public DocumentType DocumentType { get; set; }

        public Guid? VerifiedByUserId { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        
        public VerificationStatus VerificationStatus { get; set; }
        
        public Claim Claim { get; set; }
        public User UploadedByUser { get; set; }
        public User? VerifiedByUser { get; set; }
    }
}
