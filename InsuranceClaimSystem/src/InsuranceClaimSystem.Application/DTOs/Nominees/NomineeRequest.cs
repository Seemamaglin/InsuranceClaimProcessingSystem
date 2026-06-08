using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Nominees;

public class NomineeRequest
{
    public Guid PolicyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public RelationshipType Relationship { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public decimal SharePercentage { get; set; }
}