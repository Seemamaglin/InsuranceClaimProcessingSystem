namespace InsuranceClaimSystem.Domain.Enums
{
    public enum PolicyStatus
    {
        PendingApproval=1,
        Active=2,
        GracePeriod=3,
        Lapsed=4,
        Expired=5,
        Rejected=6,     //admin denied the policy approval
        Cancelled=7,   //intentioanla termination of policy
        CoverageExhausted=8 //claim amount equals to coverage amount

    }
}