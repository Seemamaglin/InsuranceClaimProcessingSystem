using InsuranceClaimSystem.Domain.Common;

namespace InsuranceClaimSystem.Domain.Events;

public class ClaimSubmittedDomainEvent : DomainEvent
{
    public Guid ClaimId { get; }
    public Guid PolicyId { get; }
    public Guid ClaimantId { get; }
    public string ClaimNumber { get; }
    public decimal ClaimedAmount { get; }

    public ClaimSubmittedDomainEvent(Guid claimId, Guid policyId, Guid claimantId, string claimNumber, decimal claimedAmount)
    {
        if (claimId == Guid.Empty)
            throw new ArgumentException("ClaimId cannot be empty.", nameof(claimId));
        if (policyId == Guid.Empty)
            throw new ArgumentException("PolicyId cannot be empty.", nameof(policyId));
        if (claimantId == Guid.Empty)
            throw new ArgumentException("ClaimantId cannot be empty.", nameof(claimantId));

        ClaimId = claimId;
        PolicyId = policyId;
        ClaimantId = claimantId;
        ClaimNumber = claimNumber ?? throw new ArgumentNullException(nameof(claimNumber));
        ClaimedAmount = claimedAmount;
        EventType = "Claim.Submitted";
    }
}