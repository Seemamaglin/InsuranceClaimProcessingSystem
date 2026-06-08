using InsuranceClaimSystem.Domain.Common;

namespace InsuranceClaimSystem.Domain.Events;

public class PolicyCreatedDomainEvent : DomainEvent
{
    public Guid PolicyId { get; }
    public Guid PolicyHolderId { get; }
    public Guid PolicyTypeId { get; }
    public string PolicyNumber { get; }

    public PolicyCreatedDomainEvent(Guid policyId, Guid policyHolderId, Guid policyTypeId, string policyNumber)
    {
        if (policyId == Guid.Empty)
            throw new ArgumentException("PolicyId cannot be empty.", nameof(policyId));
        if (policyHolderId == Guid.Empty)
            throw new ArgumentException("PolicyHolderId cannot be empty.", nameof(policyHolderId));
        if (policyTypeId == Guid.Empty)
            throw new ArgumentException("PolicyTypeId cannot be empty.", nameof(policyTypeId));

        PolicyId = policyId;
        PolicyHolderId = policyHolderId;
        PolicyTypeId = policyTypeId;
        PolicyNumber = policyNumber ?? throw new ArgumentNullException(nameof(policyNumber));
        EventType = "Policy.Created";
    }
}