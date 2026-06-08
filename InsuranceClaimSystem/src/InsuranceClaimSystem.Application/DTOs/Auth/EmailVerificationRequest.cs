namespace InsuranceClaimSystem.Application.DTOs.Auth;

public class EmailVerificationRequest
{
    public string Email { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
}