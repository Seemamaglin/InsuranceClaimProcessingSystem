using InsuranceClaimSystem.Domain.Common;

namespace InsuranceClaimSystem.Domain.Events;

public class PolicyLapsedDomainEvent : DomainEvent
{
    public Guid PolicyId { get; }
    public Guid PolicyHolderId { get; }
    public string PolicyNumber { get; }
    public DateTime LapsedAt { get; }

    public PolicyLapsedDomainEvent(Guid policyId, Guid policyHolderId, string policyNumber, DateTime lapsedAt)
    {
        if (policyId == Guid.Empty)
            throw new ArgumentException("PolicyId cannot be empty.", nameof(policyId));
        if (policyHolderId == Guid.Empty)
            throw new ArgumentException("PolicyHolderId cannot be empty.", nameof(policyHolderId));

        PolicyId = policyId;
        PolicyHolderId = policyHolderId;
        PolicyNumber = policyNumber ?? throw new ArgumentNullException(nameof(policyNumber));
        LapsedAt = lapsedAt;
        EventType = "Policy.Lapsed";
    }
}