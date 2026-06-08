namespace InsuranceClaimSystem.Application.DTOs.Nominees;

public class NomineeDto
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string AadhaarMasked { get; set; } = string.Empty;
    public decimal SharePercentage { get; set; }
    public bool IsActive { get; set; }
}