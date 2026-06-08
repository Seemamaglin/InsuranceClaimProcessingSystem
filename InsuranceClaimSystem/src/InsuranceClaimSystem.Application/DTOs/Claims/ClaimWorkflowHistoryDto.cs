using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Claims;

public class ClaimWorkflowHistoryDto
{
    public Guid Id { get; set; }
    public WorkflowActionType ActionType { get; set; }
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public string Comments { get; set; } = string.Empty;
    public string ChangedByUserName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}