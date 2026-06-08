using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Policies;

public class CreatePolicyRequest
{
    public Guid PolicyHolderId { get; set; }
    public Guid PolicyTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal CoverageAmount { get; set; }
    public decimal PremiumAmount { get; set; }
    public PremiumFrequency PremiumFrequency { get; set; }
}