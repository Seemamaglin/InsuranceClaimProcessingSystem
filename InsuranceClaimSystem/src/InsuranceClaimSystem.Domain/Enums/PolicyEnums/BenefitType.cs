namespace InsuranceClaimSystem.Domain.Enums
{
    public enum BenefitType
    {
        FixedBenefit = 1,  //full coverage amount is paid in case of Life insurance
        Reimbursement = 2  //pays only the actual cost of loss in health,auto,property insurance
    }
}