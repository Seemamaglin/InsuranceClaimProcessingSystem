using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Policies;

public class PolicyTypeDto
{
    public Guid Id { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BenefitType DefaultBenefitType { get; set; }
    public bool AllowsNomineeClaim { get; set; }
    public bool AllowsThirdPartyClaim { get; set; }
    public decimal DefaultCoverageAmount { get; set; }
    public bool IsActive { get; set; }
}