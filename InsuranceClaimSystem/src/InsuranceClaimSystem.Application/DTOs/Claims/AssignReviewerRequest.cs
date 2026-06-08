namespace InsuranceClaimSystem.Application.DTOs.Claims;

public class AssignReviewerRequest
{
    public Guid ClaimId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid AssignedByUserId { get; set; }
}