namespace InsuranceClaimSystem.Application.DTOs.Claims;

public class ClaimNoteDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsInternalOnly { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}