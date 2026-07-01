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

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var phoneCheck = await CheckPhoneNumberAvailabilityAsync(request.PhoneNumber);
                if (phoneCheck.IsFailure) return Result<AuthResponse>.Failure(phoneCheck.Error);
            }

            var roleValidation = ValidateRegistrationRole(request.Role);
            if (roleValidation.IsFailure)
                return Result<AuthResponse>.Failure(roleValidation.Error);

            var user = BuildUserEntity(request, roleValidation.Value);
            await _userRepository.AddAsync(user);

            var code = GenerateVerificationCode();
            var verification = BuildEmailVerificationCode(user, code);
            await _emailVerificationCodeRepository.AddAsync(verification);

            await _unitOfWork.SaveChangesAsync();

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

            var (accessToken, refreshToken, refreshTokenEntity, isFirstLogin) = await CompleteLoginAsync(user);
            var response = BuildAuthResponse(user, accessToken, refreshToken, refreshTokenEntity.ExpiresAt);
            response.IsFirstLogin = isFirstLogin;
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
                // To prevent user enumeration vulnerabilities, always return success
                // even if the email does not exist in our system.
                _logger.LogWarning("Forgot password - email not found: {Email}. Returning success to prevent enumeration.", request.Email);
                
                // Add a random delay to simulate DB and SMTP latency to prevent timing side-channel attacks
                var delay = new Random().Next(800, 1500);
                await Task.Delay(delay);
                
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

            bool isChangePassword = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash);
            PasswordResetToken? resetToken = null;

            if (!isChangePassword)
            {
                resetToken = await _passwordResetTokenRepository.GetActiveTokenByUserIdAsync(user.Id);
                if (resetToken == null)
                {
                    _logger.LogWarning("Password reset failed - no active token for user: {UserId}", user.Id);
                    return Result<bool>.Failure(Error.Unauthorized("InvalidToken", "Invalid old password or expired reset token."));
                }

                var tokenHash = HashToken(request.OldPassword);
                if (resetToken.Token != tokenHash)
                {
                    _logger.LogWarning("Password reset failed - token mismatch for user: {UserId}", user.Id);
                    return Result<bool>.Failure(Error.Unauthorized("InvalidToken", "Invalid old password or reset token."));
                }
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
            
            if (resetToken != null)
            {
                resetToken.IsUsed = true;
                resetToken.UsedAt = DateTime.UtcNow;
            }
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Password Reset Successful",
                    $"Hello {user.FirstName},<br><br>Your password has been successfully reset. If you did not make this change, please contact an administrator immediately.",
                    isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset success email to {Email}", user.Email);
            }

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
                
                verificationCode.IsUsed = true;
                verificationCode.UsedAt = DateTime.UtcNow;

                var newCode = GenerateVerificationCode();
                var newVerification = BuildEmailVerificationCode(user, newCode);
                await _emailVerificationCodeRepository.AddAsync(newVerification);
                await _unitOfWork.SaveChangesAsync();

                await TrySendVerificationEmailAsync(user, newCode);

                return Result<bool>.Failure(Error.Unauthorized("InvalidCode", "Invalid verification code. A new code has been sent to your email."));
            }

            user.EmailVerifiedAt = DateTime.UtcNow;
            user.RegistrationStatus = RegistrationStatus.PendingKyc;
            verificationCode.IsUsed = true;
            verificationCode.UsedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Send KYC Intimation Email
            try
            {
                string emailBody = $@"
                    <h2>Email Verified Successfully!</h2>
                    <p>Hello {user.FirstName},</p>
                    <p>Your email address has been successfully verified. However, your account is currently in <b>Pending KYC</b> status.</p>
                    <p>To fully activate your account and start applying for policies, please log in and submit the following mandatory KYC documents:</p>
                    <ul>
                        <li><b>Aadhar Card</b></li>
                        <li><b>PAN Card</b></li>
                    </ul>
                    <p>Once submitted, an Administrator will review your documents and approve your account.</p>
                    <p>Thank you,<br>Insurance Claim System Team</p>";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Action Required: Submit Your KYC Documents",
                    emailBody,
                    isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send KYC intimation email to {Email}", user.Email);
            }

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
            if (existing.RegistrationStatus == RegistrationStatus.PendingEmailVerification)
            {
                // Soft delete the existing unverified user and free up their unique constraints
                existing.Email = $"deleted_{Guid.NewGuid()}_{existing.Email}";
                existing.Username = $"deleted_{Guid.NewGuid()}_{existing.Username}";
                existing.PhoneNumber = $"deleted_{Guid.NewGuid()}_{existing.PhoneNumber}";
                await _userRepository.UpdateAsync(existing);
                await _userRepository.DeleteAsync(existing.Id);
                await _unitOfWork.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure(Error.Conflict("EmailExists", "A user with this email already exists."));
        }
        return Result<bool>.Success(true);
    }

    private async Task<Result<bool>> CheckUsernameAvailabilityAsync(string username)
    {
        var existing = await _userRepository.GetByUsernameAsync(username);
        if (existing != null)
        {
            if (existing.RegistrationStatus == RegistrationStatus.PendingEmailVerification)
            {
                existing.Email = $"deleted_{Guid.NewGuid()}_{existing.Email}";
                existing.Username = $"deleted_{Guid.NewGuid()}_{existing.Username}";
                existing.PhoneNumber = $"deleted_{Guid.NewGuid()}_{existing.PhoneNumber}";
                await _userRepository.UpdateAsync(existing);
                await _userRepository.DeleteAsync(existing.Id);
                await _unitOfWork.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure(Error.Conflict("UsernameExists", "A user with this username already exists."));
        }
        return Result<bool>.Success(true);
    }

    private async Task<Result<bool>> CheckPhoneNumberAvailabilityAsync(string phoneNumber)
    {
        var existingResult = await _userRepository.GetPagedAsync(1, 1, u => u.PhoneNumber == phoneNumber);
        var existing = existingResult.Items.FirstOrDefault();
        if (existing != null)
        {
            if (existing.RegistrationStatus == RegistrationStatus.PendingEmailVerification)
            {
                existing.Email = $"deleted_{Guid.NewGuid()}_{existing.Email}";
                existing.Username = $"deleted_{Guid.NewGuid()}_{existing.Username}";
                existing.PhoneNumber = $"deleted_{Guid.NewGuid()}_{existing.PhoneNumber}";
                await _userRepository.UpdateAsync(existing);
                await _userRepository.DeleteAsync(existing.Id);
                await _unitOfWork.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            return Result<bool>.Failure(Error.Conflict("PhoneNumberExists", "A user with this phone number already exists."));
        }
        return Result<bool>.Success(true);
    }

    private static Result<UserRole> ValidateRegistrationRole(UserRole requestedRole)
    {
        var allowedRoles = new[] { UserRole.PolicyHolder, UserRole.ClaimReviewer, UserRole.ClaimsManager, UserRole.FinanceOfficer };
        if (!allowedRoles.Contains(requestedRole))
        {
            return Result<UserRole>.Failure(
                Error.Validation("InvalidRole", $"Role '{requestedRole}' is not allowed for public registration. Admins cannot be registered publicly."));
        }
        return Result<UserRole>.Success(requestedRole);
    }

    private User BuildUserEntity(RegisterRequest request, UserRole validatedRole)
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
            Role = validatedRole,
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



    private async Task TrySendVerificationEmailAsync(User user, string code)
    {
        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Verify Your Email - Insurance Claim System",
                $"Your verification code is: <strong>{code}</strong><br/><br/>This code expires in 24 hours.",
                isHtml: true);
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
        await _userRepository.UpdateAsync(user);
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
        
        // We MUST allow login for PendingKyc, PendingApproval, KycRejected, and Approved.
        // We ONLY block PendingEmailVerification and permanently Rejected accounts.
        if (user.RegistrationStatus == RegistrationStatus.PendingEmailVerification)
        {
            return Result<User>.Failure(Error.Unauthorized("EmailNotVerified", "Please verify your email address before logging in."));
        }
        if (user.RegistrationStatus == RegistrationStatus.Rejected)
        {
            return Result<User>.Failure(Error.Unauthorized("AccountRejected", "Your account registration has been permanently rejected."));
        }

        return Result<User>.Success(user);
    }

    private async Task<(string AccessToken, string RefreshToken, RefreshToken TokenEntity, bool IsFirstLogin)> CompleteLoginAsync(User user)
    {
        var isFirstLogin = user.IsFirstLogin;
        
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.IsFirstLogin = false;

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = BuildRefreshTokenEntity(user, refreshToken);
        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return (accessToken, refreshToken, refreshTokenEntity, isFirstLogin);
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
                $"Click the link to reset your password: <a href='http://localhost:4200/reset-password?token={resetToken}&email={user.Email}'>Reset Password</a><br/><br/>This link expires in 30 minutes.");
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