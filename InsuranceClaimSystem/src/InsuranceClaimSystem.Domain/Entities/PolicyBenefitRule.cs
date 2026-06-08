using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Domain.Entities;
public class PolicyBenefitRule : BaseEntity
{
    public Guid PolicyTypeId { get; set; }
    public Guid ClaimTypeId { get; set; }

    public decimal CoPayPercent { get; set; }           // ← ADD THIS (missing from original)
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

    // Navigation
    public PolicyType PolicyType { get; set; } = null!;
    public ClaimType ClaimType { get; set; } = null!;
}