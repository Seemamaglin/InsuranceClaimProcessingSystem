namespace InsuranceClaimSystem.Application.DTOs.Policies;

public class PolicyBenefitRuleDto
{
    public Guid Id { get; set; }
    public Guid PolicyTypeId { get; set; }
    public Guid ClaimTypeId { get; set; }
    public decimal CoPayPercent { get; set; }
    public decimal MaxClaimablePercent { get; set; }
    public decimal SubLimitAmount { get; set; }
    public string SubLimitDescription { get; set; } = string.Empty;
    public decimal DeductibleAmount { get; set; }
    public int WaitingPeriodDays { get; set; }
    public int IntimationDeadlineDays { get; set; }
    public bool RequiresPoliceReport { get; set; }
    public bool RequiresMedicalCertificate { get; set; }
    public bool RequiresDeathCertificate { get; set; }
    public bool IsActive { get; set; }
}