namespace InsuranceClaimSystem.Application.DTOs.Policies;

public class PayPremiumRequest
{
    public Guid PolicyId { get; set; }
    public decimal Amount { get; set; }
}