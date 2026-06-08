namespace InsuranceClaimSystem.Domain.Enums
{
    public enum WorkflowActionType
    {
        StatusChange = 1,
        Assignment = 2,
        DocumentRequest = 3,
        Approval = 4,
        Rejection = 5,
        Escalation = 6
    }
}