using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Policies;

public class ApplyForPolicyRequest
{
    public Guid PolicyTypeId { get; set; }
    public DateTime StartDate { get; set; }

    public decimal CoverageAmount { get; set; }
    public decimal PremiumAmount { get; set; }
    public PremiumFrequency PremiumFrequency { get; set; }
    
    public List<PolicyNomineeRequest> Nominees { get; set; } = new();
}
