using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Auth;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Result<Token>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<Result<bool>> VerifyEmailAsync(EmailVerificationRequest request);
}