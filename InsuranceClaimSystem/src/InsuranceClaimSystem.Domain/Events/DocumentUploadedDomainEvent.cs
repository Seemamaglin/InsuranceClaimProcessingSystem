using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Events;

public class DocumentUploadedDomainEvent : DomainEvent
{
    public Guid DocumentId { get; }
    public Guid ClaimId { get; }
    public Guid UploadedByUserId { get; }
    public DocumentType DocumentType { get; }
    public string FileName { get; }

    public DocumentUploadedDomainEvent(Guid documentId, Guid claimId, Guid uploadedByUserId, DocumentType documentType, string fileName)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId cannot be empty.", nameof(documentId));
        if (claimId == Guid.Empty)
            throw new ArgumentException("ClaimId cannot be empty.", nameof(claimId));
        if (uploadedByUserId == Guid.Empty)
            throw new ArgumentException("UploadedByUserId cannot be empty.", nameof(uploadedByUserId));

        DocumentId = documentId;
        ClaimId = claimId;
        UploadedByUserId = uploadedByUserId;
        DocumentType = documentType;
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        EventType = "Document.Uploaded";
    }
}