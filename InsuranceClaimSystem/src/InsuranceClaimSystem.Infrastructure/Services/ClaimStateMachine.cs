using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Domain.Exceptions;

namespace InsuranceClaimSystem.Infrastructure.Services;

public static class ClaimStateMachine
{
    private static readonly Dictionary<ClaimStatus, HashSet<ClaimStatus>> ValidTransitions = new()
    {
        [ClaimStatus.Draft] = new() { ClaimStatus.Submitted },
        [ClaimStatus.Submitted] = new() { ClaimStatus.UnderReview },
        [ClaimStatus.UnderReview] = new() { ClaimStatus.DocumentsPending, ClaimStatus.Approved, ClaimStatus.Rejected },
        [ClaimStatus.DocumentsPending] = new() { ClaimStatus.UnderReview },
        [ClaimStatus.Approved] = new() { ClaimStatus.Closed },
        [ClaimStatus.Rejected] = new() { ClaimStatus.Closed },
        [ClaimStatus.Closed] = new() // terminal
    };

    public static bool CanTransition(ClaimStatus from, ClaimStatus to)
    {
        return ValidTransitions.TryGetValue(from, out var validTargets) && validTargets.Contains(to);
    }

    public static void ValidateTransition(ClaimStatus from, ClaimStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new BusinessRuleException($"Invalid claim status transition from {from} to {to}.");
        }
    }
}