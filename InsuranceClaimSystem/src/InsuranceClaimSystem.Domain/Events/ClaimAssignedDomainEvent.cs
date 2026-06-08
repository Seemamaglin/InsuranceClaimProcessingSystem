using InsuranceClaimSystem.Domain.Common;

namespace InsuranceClaimSystem.Domain.Events;

public class ClaimAssignedDomainEvent : DomainEvent
{
    public Guid ClaimId { get; }
    public Guid? AssignedReviewerId { get; }
    public Guid? AssignedManagerId { get; }
    public Guid AssignedByUserId { get; }

    public ClaimAssignedDomainEvent(Guid claimId, Guid? assignedReviewerId, Guid? assignedManagerId, Guid assignedByUserId)
    {
        if (claimId == Guid.Empty)
            throw new ArgumentException("ClaimId cannot be empty.", nameof(claimId));
        if (assignedByUserId == Guid.Empty)
            throw new ArgumentException("AssignedByUserId cannot be empty.", nameof(assignedByUserId));

        ClaimId = claimId;
        AssignedReviewerId = assignedReviewerId;
        AssignedManagerId = assignedManagerId;
        AssignedByUserId = assignedByUserId;
        EventType = "Claim.Assigned";
    }
}