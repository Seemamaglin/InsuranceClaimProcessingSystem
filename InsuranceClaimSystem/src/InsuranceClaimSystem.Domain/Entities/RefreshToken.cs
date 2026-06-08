namespace InsuranceClaimSystem.Domain.Entities;

public class RefreshToken   // No BaseEntity — this is auth infrastructure, not a domain entity
{
    public Guid TokenId { get; set; }
    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;   // stored hashed
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }          // "Used" / "LoggedOut" / "SuspiciousReuse"
    public string? ReplacedByToken { get; set; }        // successor token hash
    public DateTime CreatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}