using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Auth;

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiry { get; set; }
    public UserRole Role { get; set; }
    public bool IsFirstLogin { get; set; }
}