namespace InsuranceClaimSystem.Domain.Entities;

public class PasswordResetToken   // No BaseEntity
{
    public Guid ResetTokenId { get; set; }
    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;   // SHA-256 hashed
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string RequestedFromIp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}