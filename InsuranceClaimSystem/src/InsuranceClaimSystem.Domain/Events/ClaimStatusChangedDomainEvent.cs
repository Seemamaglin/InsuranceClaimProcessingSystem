using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Events;

public class ClaimStatusChangedDomainEvent : DomainEvent
{
    public Guid ClaimId { get; }
    public ClaimStatus OldStatus { get; }
    public ClaimStatus NewStatus { get; }
    public Guid ChangedByUserId { get; }
    public string? Comments { get; }

    public ClaimStatusChangedDomainEvent(Guid claimId, ClaimStatus oldStatus, ClaimStatus newStatus, Guid changedByUserId, string? comments)
    {
        if (claimId == Guid.Empty)
            throw new ArgumentException("ClaimId cannot be empty.", nameof(claimId));
        if (changedByUserId == Guid.Empty)
            throw new ArgumentException("ChangedByUserId cannot be empty.", nameof(changedByUserId));

        ClaimId = claimId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
        Comments = comments;
        EventType = "Claim.StatusChanged";
    }
}