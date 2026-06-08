using InsuranceClaimSystem.Domain.Common;

namespace InsuranceClaimSystem.Domain.Entities;

public class HealthRecord : BaseEntity
{
    public Guid PolicyId { get; set; }
    public Guid PolicyHolderId { get; set; }

    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
    public string? KnownConditions { get; set; }
    public bool IsSmoker { get; set; }
    public string? AlcoholConsumption { get; set; }  // None / Occasional / Regular
    public DateTime DeclaredAt { get; set; }

    // Navigation
    public Policy Policy { get; set; } = null!;
    public User PolicyHolder { get; set; } = null!;
}