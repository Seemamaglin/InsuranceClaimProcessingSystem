namespace InsuranceClaimSystem.Domain.Enums
{
    public enum RegistrationStatus
    {
        NA=1,
        PendingEmailVerification=2,
        PendingApproval=3,
        Approved=4,
        Rejected=5
    }
}