using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Claims;

public class SaveClaimDraftRequest
{
    public Guid? ClaimId { get; set; }
    public Guid PolicyId { get; set; }
    public Guid? ClaimTypeId { get; set; }
    public DateTime? IncidentDate { get; set; }
    public string? IncidentDescription { get; set; }
    public string? IncidentLocation { get; set; }
    public decimal? ClaimedAmount { get; set; }
    public Guid? NomineeId { get; set; }
    public ClaimantType? ClaimantType { get; set; }
}
