namespace InsuranceClaimSystem.Application.DTOs.Claims;

public class ThirdPartyClaimantDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public decimal EstimatedDamageAmount { get; set; }
    public string? PoliceReportNumber { get; set; }
}