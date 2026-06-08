using InsuranceClaimSystem.Domain.Common;

namespace InsuranceClaimSystem.Domain.Events;

public class PolicyApprovedDomainEvent : DomainEvent
{
    public Guid PolicyId { get; }
    public Guid PolicyHolderId { get; }
    public string PolicyNumber { get; }
    public DateTime StartDate { get; }

    public PolicyApprovedDomainEvent(Guid policyId, Guid policyHolderId, string policyNumber, DateTime startDate)
    {
        if (policyId == Guid.Empty)
            throw new ArgumentException("PolicyId cannot be empty.", nameof(policyId));
        if (policyHolderId == Guid.Empty)
            throw new ArgumentException("PolicyHolderId cannot be empty.", nameof(policyHolderId));

        PolicyId = policyId;
        PolicyHolderId = policyHolderId;
        PolicyNumber = policyNumber ?? throw new ArgumentNullException(nameof(policyNumber));
        StartDate = startDate;
        EventType = "Policy.Approved";
    }
}