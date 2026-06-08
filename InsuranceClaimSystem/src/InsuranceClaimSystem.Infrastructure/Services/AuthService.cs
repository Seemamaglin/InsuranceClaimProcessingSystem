using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Auth;
using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IEmailVerificationCodeRepository _emailVerificationCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IEmailVerificationCodeRepository emailVerificationCodeRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _emailVerificationCodeRepository = emailVerificationCodeRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return Result<AuthResponse>.Failure(
                    Error.Conflict("EmailExists", "A user with this email already exists."));
            }

            var existingUsername = await _userRepository.GetByUsernameAsync(request.UserName);
            if (existingUsername != null)
            {
                return Result<AuthResponse>.Failure(
                    Error.Conflict("UsernameExists", "A user with this username already exists."));
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Email = request.Email,
                Username = request.UserName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
                PhoneNumber = request.PhoneNumber,
                Role = UserRole.PolicyHolder,
                RegistrationStatus = RegistrationStatus.PendingEmailVerification,
                IsActive = false,
                IsFirstLogin = true,
                FailedLoginAttempts = 0
            };

            await _userRepository.AddAsync(user);

            var verificationCode = new Random().Next(100000, 999999).ToString();
            var codeHash = HashToken(verificationCode);
            var emailVerification = new EmailVerificationCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CodeHash = codeHash,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            await _emailVerificationCodeRepository.AddAsync(emailVerification);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Verify Your Email - Insurance Claim System",
                    $"Your verification code is: <strong>{verificationCode}</strong><br/><br/>This code expires in 24 hours.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
            }

            return Result<AuthResponse>.Success(new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Token = string.Empty,
                RefreshToken = string.Empty,
                RefreshTokenExpiry = DateTime.MinValue,
                Role = user.Role,
                IsFirstLogin = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return Result<AuthResponse>.Failure(
                Error.Validation("RegistrationFailed", "An error occurred during registration."));
        }
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.EmailOrUsername);
            if (user == null)
            {
                user = await _userRepository.GetByUsernameAsync(request.EmailOrUsername);
            }

            if (user == null)
            {
                return Result<AuthResponse>.Failure(
                    Error.Unauthorized("InvalidCredentials", "Invalid email/username or password."));
            }

            if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
            {
                var remainingMinutes = (user.LockoutUntil.Value - DateTime.UtcNow).TotalMinutes;
                return Result<AuthResponse>.Failure(
                    Error.Unauthorized("AccountLocked", $"Account is locked. Try again in {(int)remainingMinutes} minutes."));
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
                    _logger.LogWarning("User {UserId} locked out due to too many failed login attempts", user.Id);
                }
                await _unitOfWork.SaveChangesAsync();

                return Result<AuthResponse>.Failure(
                    Error.Unauthorized("InvalidCredentials", "Invalid email/username or password."));
            }

            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            user.LastLoginAt = DateTime.UtcNow;

            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenHash = HashToken(refreshToken);

            var refreshTokenEntity = new RefreshToken
            {
                TokenId = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return Result<AuthResponse>.Success(new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Token = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiry = refreshTokenEntity.ExpiresAt,
                Role = user.Role,
                IsFirstLogin = user.IsFirstLogin
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {EmailOrUsername}", request.EmailOrUsername);
            return Result<AuthResponse>.Failure(
                Error.Validation("LoginFailed", "An error occurred during login."));
        }
    }

    public async Task<Result<Token>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Result<Token>.Failure(
                    Error.Unauthorized("InvalidToken", "Refresh token is required."));
            }

            var refreshTokenHash = HashToken(request.RefreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenHash);

            if (storedToken == null)
            {
                return Result<Token>.Failure(
                    Error.Unauthorized("InvalidToken", "Invalid refresh token."));
            }

            if (storedToken.IsRevoked)
            {
                return Result<Token>.Failure(
                    Error.Unauthorized("TokenRevoked", "Refresh token has been revoked."));
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return Result<Token>.Failure(
                    Error.Unauthorized("TokenExpired", "Refresh token has expired."));
            }

            var user = await _userRepository.GetByIdAsync(storedToken.UserId);
            if (user == null)
            {
                return Result<Token>.Failure(
                    Error.Unauthorized("UserNotFound", "User not found."));
            }

            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedReason = "Used";

            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var newRefreshTokenHash = HashToken(newRefreshToken);

            var newTokenEntity = new RefreshToken
            {
                TokenId = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.UpdateAsync(storedToken);
            await _refreshTokenRepository.AddAsync(newTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return Result<Token>.Success(new Token(
                newAccessToken,
                newRefreshToken,
                DateTime.UtcNow.AddMinutes(15),
                newTokenEntity.ExpiresAt,
                "Bearer",
                900));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return Result<Token>.Failure(
                Error.Validation("RefreshFailed", "An error occurred during token refresh."));
        }
    }

    public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return Result<bool>.Success(true);
            }

            var resetToken = Guid.NewGuid().ToString("N");
            var tokenHash = HashToken(resetToken);

            var passwordResetToken = new PasswordResetToken
            {
                ResetTokenId = Guid.NewGuid(),
                UserId = user.Id,
                Token = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false,
                RequestedFromIp = string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _passwordResetTokenRepository.AddAsync(passwordResetToken);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Reset Your Password - Insurance Claim System",
                    $"Click the link to reset your password: <a href='https://your-frontend-url/reset-password?token={resetToken}&email={user.Email}'>Reset Password</a><br/><br/>This link expires in 30 minutes.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for {Email}", request.Email);
            return Result<bool>.Failure(
                Error.Validation("ForgotPasswordFailed", "An error occurred during password reset request."));
        }
    }

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            var resetToken = await _passwordResetTokenRepository.GetActiveTokenByUserIdAsync(user.Id);
            if (resetToken == null)
            {
                return Result<bool>.Failure(
                    Error.Unauthorized("InvalidToken", "Invalid or expired reset token."));
            }

            var tokenHash = HashToken(request.OldPassword);
            if (resetToken.Token != tokenHash)
            {
                return Result<bool>.Failure(
                    Error.Unauthorized("InvalidToken", "Invalid reset token."));
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for {Email}", request.Email);
            return Result<bool>.Failure(
                Error.Validation("ResetPasswordFailed", "An error occurred during password reset."));
        }
    }

    public async Task<Result<bool>> VerifyEmailAsync(EmailVerificationRequest request)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            var verificationCode = await _emailVerificationCodeRepository.GetActiveCodeByUserIdAsync(user.Id);
            if (verificationCode == null)
            {
                return Result<bool>.Failure(
                    Error.Unauthorized("InvalidCode", "Verification code not found or expired."));
            }

            var codeHash = HashToken(request.VerificationCode);
            if (verificationCode.CodeHash != codeHash)
            {
                return Result<bool>.Failure(
                    Error.Unauthorized("InvalidCode", "Invalid verification code."));
            }

            user.EmailVerifiedAt = DateTime.UtcNow;
            user.RegistrationStatus = RegistrationStatus.PendingApproval;
            verificationCode.IsUsed = true;
            verificationCode.UsedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification for {Email}", request.Email);
            return Result<bool>.Failure(
                Error.Validation("VerificationFailed", "An error occurred during email verification."));
        }
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}