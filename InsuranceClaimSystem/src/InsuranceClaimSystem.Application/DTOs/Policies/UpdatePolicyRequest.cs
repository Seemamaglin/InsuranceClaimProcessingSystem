using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Policies;

public class UpdatePolicyRequest
{
    public Guid PolicyId { get; set; }
    public decimal? CoverageAmount { get; set; }
    public decimal? PremiumAmount { get; set; }
    public DateTime? EndDate { get; set; }
    public PremiumFrequency? PremiumFrequency { get; set; }
}