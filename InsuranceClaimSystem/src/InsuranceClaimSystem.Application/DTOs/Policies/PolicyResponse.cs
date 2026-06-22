using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Policies;

public class PolicyResponse
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Guid PolicyHolderId { get; set; }
    public string PolicyHolderName { get; set; } = string.Empty;
    public Guid PolicyTypeId { get; set; }
    public string PolicyTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal CoverageAmount { get; set; }
    public decimal RemainingCoverageAmount { get; set; }
    public decimal PremiumAmount { get; set; }
    public PremiumFrequency PremiumFrequency { get; set; }
    public DateTime NextPremiumDueDate { get; set; }
    public DateTime? LastPremiumPaidDate { get; set; }
    public int GracePeriodDays { get; set; }
    public PolicyStatus Status { get; set; }

    public double EstimatedYearsToComplete
    {
        get
        {
            if (PremiumAmount <= 0 || CoverageAmount <= 0) return 0;
            int paymentsPerYear = PremiumFrequency switch
            {
                PremiumFrequency.Monthly => 12,
                PremiumFrequency.Quarterly => 4,
                PremiumFrequency.HalfYearly => 2,
                PremiumFrequency.Annually => 1,
                _ => 1
            };
            decimal yearlyPremium = PremiumAmount * paymentsPerYear;
            return (double)Math.Round(CoverageAmount / yearlyPremium, 2);
        }
    }
}