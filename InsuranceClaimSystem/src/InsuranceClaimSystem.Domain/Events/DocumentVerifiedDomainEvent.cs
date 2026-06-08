using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Events;

public class DocumentVerifiedDomainEvent : DomainEvent
{
    public Guid DocumentId { get; }
    public Guid ClaimId { get; }
    public Guid VerifiedByUserId { get; }
    public VerificationStatus VerificationStatus { get; }
    public string? RejectionReason { get; }

    public DocumentVerifiedDomainEvent(Guid documentId, Guid claimId, Guid verifiedByUserId, VerificationStatus verificationStatus, string? rejectionReason)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId cannot be empty.", nameof(documentId));
        if (claimId == Guid.Empty)
            throw new ArgumentException("ClaimId cannot be empty.", nameof(claimId));
        if (verifiedByUserId == Guid.Empty)
            throw new ArgumentException("VerifiedByUserId cannot be empty.", nameof(verifiedByUserId));

        DocumentId = documentId;
        ClaimId = claimId;
        VerifiedByUserId = verifiedByUserId;
        VerificationStatus = verificationStatus;
        RejectionReason = rejectionReason;
        EventType = "Document.Verified";
    }
}