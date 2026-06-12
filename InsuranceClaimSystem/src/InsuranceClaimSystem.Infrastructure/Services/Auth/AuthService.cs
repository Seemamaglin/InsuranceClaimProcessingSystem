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
        _logger.LogInformation("Starting user registration for email: {Email}", request.Email);
        try
        {
            var emailCheck = await CheckEmailAvailabilityAsync(request.Email);
            if (emailCheck.IsFailure) return Result<AuthResponse>.Failure(emailCheck.Error);

            var usernameCheck = await CheckUsernameAvailabilityAsync(request.UserName);
            if (usernameCheck.IsFailure) return Result<AuthResponse>.Failure(usernameCheck.Error);

            var user = BuildUserEntity(request);
            await _userRepository.AddAsync(user);

            var code = GenerateVerificationCode();
            var verification = BuildEmailVerificationCode(user, code);
            await _emailVerificationCodeRepository.AddAsync(verification);

            await AutoVerifyAndApproveAsync(user, verification);

            await TrySendVerificationEmailAsync(user, code);

            var response = BuildAuthResponse(user, string.Empty, string.Empty);
            _logger.LogInformation("User registered successfully: {UserId}", user.Id);
            return Result<AuthResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return Result<AuthResponse>.Failure(Error.Validation("RegistrationFailed", "An error occurred during registration."));
        }
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Starting login for: {EmailOrUsername}", request.EmailOrUsername);
        try
        {
            var user = await FindUserByEmailOrUsernameAsync(request.EmailOrUsername);
            var validation = ValidateLoginUser(user);
            if (validation.IsFailure) return Result<AuthResponse>.Failure(validation.Error);

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user!.PasswordHash))
            {
                await HandleFailedLoginAsync(user);
                return Result<AuthResponse>.Failure(Error.Unauthorized("InvalidCredentials", "Invalid email/username or password."));
            }

            var (accessToken, refreshToken, refreshTokenEntity) = await CompleteLoginAsync(user);
            var response = BuildAuthResponse(user, accessToken, refreshToken, refreshTokenEntity.ExpiresAt);
            _logger.LogInformation("Login succeeded for user: {UserId}", user.Id);
            return Result<AuthResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {EmailOrUsername}", request.EmailOrUsername);
            return Result<AuthResponse>.Failure(Error.Validation("LoginFailed", "An error occurred during login."));
        }
    }

    public async Task<Result<Token>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        _logger.LogInformation("Starting token refresh");
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Result<Token>.Failure(Error.Unauthorized("InvalidToken", "Refresh token is required."));
            }

            var tokenValidation = await ValidateRefreshTokenAsync(request.RefreshToken);
            if (tokenValidation.IsFailure)
            {
                _logger.LogWarning("Token refresh failed - invalid token");
                return Result<Token>.Failure(tokenValidation.Error);
            }

            var (user, storedToken) = tokenValidation.Value;
            await RevokeTokenAsync(storedToken, "Used");

            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            var newTokenEntity = BuildRefreshTokenEntity(user, newRefreshToken);
            await _refreshTokenRepository.AddAsync(newTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var result = BuildTokenResult(newAccessToken, newRefreshToken, newTokenEntity.ExpiresAt);
            _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);
            return Result<Token>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return Result<Token>.Failure(Error.Validation("RefreshFailed", "An error occurred during token refresh."));
        }
    }

    public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        _logger.LogInformation("Starting forgot password for email: {Email}", request.Email);
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogInformation("Forgot password - email not found, returning success");
                return Result<bool>.Success(true);
            }

            var resetToken = Guid.NewGuid().ToString("N");
            var passwordResetToken = BuildPasswordResetToken(user, resetToken);
            await _passwordResetTokenRepository.AddAsync(passwordResetToken);
            await _unitOfWork.SaveChangesAsync();

            await TrySendPasswordResetEmailAsync(user, resetToken);
            _logger.LogInformation("Forgot password succeeded for email: {Email}", request.Email);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for {Email}", request.Email);
            return Result<bool>.Failure(Error.Validation("ForgotPasswordFailed", "An error occurred during password reset request."));
        }
    }

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        _logger.LogInformation("Starting password reset for email: {Email}", request.Email);
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Password reset failed - user not found: {Email}", request.Email);
                return Result<bool>.Failure(Error.NotFound("UserNotFound", "User not found."));
            }

            var resetToken = await _passwordResetTokenRepository.GetActiveTokenByUserIdAsync(user.Id);
            if (resetToken == null)
            {
                _logger.LogWarning("Password reset failed - no active token for user: {UserId}", user.Id);
                return Result<bool>.Failure(Error.Unauthorized("InvalidToken", "Invalid or expired reset token."));
            }

            var tokenHash = HashToken(request.OldPassword);
            if (resetToken.Token != tokenHash)
            {
                _logger.LogWarning("Password reset failed - token mismatch for user: {UserId}", user.Id);
                return Result<bool>.Failure(Error.Unauthorized("InvalidToken", "Invalid reset token."));
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password reset succeeded for email: {Email}", request.Email);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for {Email}", request.Email);
            return Result<bool>.Failure(Error.Validation("ResetPasswordFailed", "An error occurred during password reset."));
        }
    }

    public async Task<Result<bool>> VerifyEmailAsync(EmailVerificationRequest request)
    {
        _logger.LogInformation("Starting email verification for email: {Email}", request.Email);
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Email verification failed - user not found: {Email}", request.Email);
                return Result<bool>.Failure(Error.NotFound("UserNotFound", "User not found."));
            }

            var verificationCode = await _emailVerificationCodeRepository.GetActiveCodeByUserIdAsync(user.Id);
            if (verificationCode == null)
            {
                _logger.LogWarning("Email verification failed - no active code for user: {UserId}", user.Id);
                return Result<bool>.Failure(Error.Unauthorized("InvalidCode", "Verification code not found or expired."));
            }

            var codeHash = HashToken(request.VerificationCode);
            if (verificationCode.CodeHash != codeHash)
            {
                _logger.LogWarning("Email verification failed - code mismatch for user: {UserId}", user.Id);
                return Result<bool>.Failure(Error.Unauthorized("InvalidCode", "Invalid verification code."));
            }

            user.EmailVerifiedAt = DateTime.UtcNow;
            user.RegistrationStatus = RegistrationStatus.PendingApproval;
            verificationCode.IsUsed = true;
            verificationCode.UsedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Email verification succeeded for email: {Email}", request.Email);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification for {Email}", request.Email);
            return Result<bool>.Failure(Error.Validation("VerificationFailed", "An error occurred during email verification."));
        }
    }

    // Private helper methods

    private async Task<Result<bool>> CheckEmailAvailabilityAsync(string email)
    {
        var existing = await _userRepository.GetByEmailAsync(email);
        if (existing != null)
        {
            return Result<bool>.Failure(Error.Conflict("EmailExists", "A user with this email already exists."));
        }
        return Result<bool>.Success(true);
    }

    private async Task<Result<bool>> CheckUsernameAvailabilityAsync(string username)
    {
        var existing = await _userRepository.GetByUsernameAsync(username);
        if (existing != null)
        {
            return Result<bool>.Failure(Error.Conflict("UsernameExists", "A user with this username already exists."));
        }
        return Result<bool>.Success(true);
    }

    private User BuildUserEntity(RegisterRequest request)
    {
        return new User
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
    }

    private EmailVerificationCode BuildEmailVerificationCode(User user, string code)
    {
        return new EmailVerificationCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = HashToken(code),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task AutoVerifyAndApproveAsync(User user, EmailVerificationCode verification)
    {
        verification.IsUsed = true;
        verification.UsedAt = DateTime.UtcNow;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.RegistrationStatus = RegistrationStatus.PendingApproval;
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task TrySendVerificationEmailAsync(User user, string code)
    {
        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Verify Your Email - Insurance Claim System",
                $"Your verification code is: <strong>{code}</strong><br/><br/>This code expires in 24 hours.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
        }
    }

    private AuthResponse BuildAuthResponse(User user, string token, string refreshToken, DateTime? refreshTokenExpiry = null)
    {
        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Token = token,
            RefreshToken = refreshToken,
            RefreshTokenExpiry = refreshTokenExpiry ?? DateTime.MinValue,
            Role = user.Role,
            IsFirstLogin = user.IsFirstLogin
        };
    }

    private async Task<User?> FindUserByEmailOrUsernameAsync(string emailOrUsername)
    {
        var user = await _userRepository.GetByEmailAsync(emailOrUsername);
        if (user == null)
        {
            user = await _userRepository.GetByUsernameAsync(emailOrUsername);
        }
        return user;
    }

    private async Task HandleFailedLoginAsync(User user)
    {
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= 5)
        {
            user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
            _logger.LogWarning("User {UserId} locked out due to too many failed login attempts", user.Id);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    private Result<User> ValidateLoginUser(User? user)
    {
        if (user == null)
        {
            return Result<User>.Failure(Error.Unauthorized("InvalidCredentials", "Invalid email/username or password."));
        }
        if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
        {
            var remainingMinutes = (user.LockoutUntil.Value - DateTime.UtcNow).TotalMinutes;
            return Result<User>.Failure(Error.Unauthorized("AccountLocked", $"Account is locked. Try again in {(int)remainingMinutes} minutes."));
        }
        return Result<User>.Success(user);
    }

    private async Task<(string AccessToken, string RefreshToken, RefreshToken TokenEntity)> CompleteLoginAsync(User user)
    {
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = BuildRefreshTokenEntity(user, refreshToken);
        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        return (accessToken, refreshToken, refreshTokenEntity);
    }

    private RefreshToken BuildRefreshTokenEntity(User user, string refreshToken)
    {
        return new RefreshToken
        {
            TokenId = Guid.NewGuid(),
            UserId = user.Id,
            Token = HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<Result<(User User, RefreshToken Token)>> ValidateRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(tokenHash);
        if (storedToken == null)
        {
            return Result<(User, RefreshToken)>.Failure(Error.Unauthorized("InvalidToken", "Invalid refresh token."));
        }
        if (storedToken.IsRevoked)
        {
            return Result<(User, RefreshToken)>.Failure(Error.Unauthorized("TokenRevoked", "Refresh token has been revoked."));
        }
        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<(User, RefreshToken)>.Failure(Error.Unauthorized("TokenExpired", "Refresh token has expired."));
        }
        var user = await _userRepository.GetByIdAsync(storedToken.UserId);
        if (user == null)
        {
            return Result<(User, RefreshToken)>.Failure(Error.Unauthorized("UserNotFound", "User not found."));
        }
        return Result<(User, RefreshToken)>.Success((user, storedToken));
    }

    private async Task RevokeTokenAsync(RefreshToken token, string reason)
    {
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedReason = reason;
        await _refreshTokenRepository.UpdateAsync(token);
    }

    private PasswordResetToken BuildPasswordResetToken(User user, string resetToken)
    {
        return new PasswordResetToken
        {
            ResetTokenId = Guid.NewGuid(),
            UserId = user.Id,
            Token = HashToken(resetToken),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsUsed = false,
            RequestedFromIp = string.Empty,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task TrySendPasswordResetEmailAsync(User user, string resetToken)
    {
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
    }

    private Token BuildTokenResult(string accessToken, string refreshToken, DateTime refreshTokenExpiry)
    {
        return new Token(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15), refreshTokenExpiry, "Bearer", 900);
    }

    private static string GenerateVerificationCode()
    {
        return new Random().Next(100000, 999999).ToString();
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}