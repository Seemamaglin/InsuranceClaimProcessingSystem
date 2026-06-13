using FluentAssertions;
using InsuranceClaimSystem.Application.DTOs.Auth;
using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepositoryMock;
    private readonly Mock<IEmailVerificationCodeRepository> _emailVerificationCodeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _passwordResetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        _emailVerificationCodeRepositoryMock = new Mock<IEmailVerificationCodeRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _passwordResetTokenRepositoryMock.Object,
            _emailVerificationCodeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _jwtTokenServiceMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1),
            Email = "john.doe@example.com",
            UserName = "johndoe",
            Password = "Password123!",
            PhoneNumber = "1234567890"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(request.UserName)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _emailVerificationCodeRepositoryMock.Setup(x => x.AddAsync(It.IsAny<EmailVerificationCode>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com",
            UserName = "johndoe",
            Password = "Password123!",
            PhoneNumber = "1234567890"
        };

        var existingUser = new User { Id = Guid.NewGuid(), Email = request.Email };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EmailExists");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "john.doe@example.com",
            Username = "johndoe",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = passwordHash,
            Role = UserRole.PolicyHolder,
            IsFirstLogin = true,
            FailedLoginAttempts = 0,
            IsActive = true
        };

        var request = new LoginRequest { EmailOrUsername = "john.doe@example.com", Password = "Password123!" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.EmailOrUsername)).ReturnsAsync(user);
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("access-token");
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        _refreshTokenRepositoryMock.Setup(x => x.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("access-token");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!", 12);
        var user = new User { Id = Guid.NewGuid(), Email = "john.doe@example.com", PasswordHash = passwordHash, FailedLoginAttempts = 0, IsActive = true };
        var request = new LoginRequest { EmailOrUsername = "john.doe@example.com", Password = "WrongPassword123!" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.EmailOrUsername)).ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidCredentials");
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Login_WithLockedAccount_ShouldReturnFailure()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "john.doe@example.com", LockoutUntil = DateTime.UtcNow.AddMinutes(10), IsActive = true };
        var request = new LoginRequest { EmailOrUsername = "john.doe@example.com", Password = "Password123!" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.EmailOrUsername)).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AccountLocked");
    }

    [Fact]
    public async Task Login_After5FailedAttempts_ShouldLockAccount()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!", 12);
        var user = new User { Id = Guid.NewGuid(), Email = "john.doe@example.com", PasswordHash = passwordHash, FailedLoginAttempts = 4, IsActive = true };
        var request = new LoginRequest { EmailOrUsername = "john.doe@example.com", Password = "WrongPassword!" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.EmailOrUsername)).ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(5);
        user.LockoutUntil.Should().NotBeNull();
    }

    [Fact]
    public async Task ForgotPassword_WithExistingEmail_ShouldSendResetToken()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "john.doe@example.com" };
        var request = new ForgotPasswordRequest { Email = "john.doe@example.com" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(user);
        _passwordResetTokenRepositoryMock.Setup(x => x.AddAsync(It.IsAny<PasswordResetToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.ForgotPasswordAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistingEmail_ShouldStillReturnSuccess()
    {
        // Arrange
        var request = new ForgotPasswordRequest { Email = "nonexistent@example.com" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.ForgotPasswordAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "john.doe@example.com", Username = "johndoe", FirstName = "John", LastName = "Doe", Role = UserRole.PolicyHolder };
        var refreshToken = new RefreshToken { TokenId = Guid.NewGuid(), UserId = userId, Token = "hashed-token", ExpiresAt = DateTime.UtcNow.AddDays(7), IsRevoked = false };
        var request = new RefreshTokenRequest { RefreshToken = "valid-refresh-token" };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync(refreshToken);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _refreshTokenRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
        _refreshTokenRepositoryMock.Setup(x => x.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("new-access-token");
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh-token");
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("new-access-token");
    }

    [Fact]
    public async Task RefreshToken_WithRevokedToken_ShouldReturnFailure()
    {
        // Arrange
        var revokedToken = new RefreshToken { TokenId = Guid.NewGuid(), UserId = Guid.NewGuid(), Token = "hashed-token", IsRevoked = true };
        var request = new RefreshTokenRequest { RefreshToken = "revoked-token" };
        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(It.IsAny<string>())).ReturnsAsync(revokedToken);

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("TokenRevoked");
    }

    [Fact]
    public async Task VerifyEmail_WithValidCode_ShouldVerifyEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "john.doe@example.com", RegistrationStatus = RegistrationStatus.PendingEmailVerification };
        var verificationCode = new EmailVerificationCode { Id = Guid.NewGuid(), UserId = userId, CodeHash = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=", ExpiresAt = DateTime.UtcNow.AddHours(24), IsUsed = false };
        var request = new EmailVerificationRequest { Email = "john.doe@example.com", VerificationCode = "123456" };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(user);
        _emailVerificationCodeRepositoryMock.Setup(x => x.GetActiveCodeByUserIdAsync(userId)).ReturnsAsync(verificationCode);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _authService.VerifyEmailAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.EmailVerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var request = new LoginRequest { EmailOrUsername = "nonexistent@example.com", Password = "Password123!" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.EmailOrUsername)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(request.EmailOrUsername)).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidCredentials");
    }

    [Fact]
    public async Task Register_WithAllowedRole_ShouldCreateUserWithThatRole()
    {
        var request = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1),
            Email = "john.doe@example.com",
            UserName = "johndoe",
            Password = "Password123!",
            PhoneNumber = "1234567890",
            Role = UserRole.PolicyHolder
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync(request.UserName)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _emailVerificationCodeRepositoryMock.Setup(x => x.AddAsync(It.IsAny<EmailVerificationCode>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

        var result = await _authService.RegisterAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Role.Should().Be(UserRole.PolicyHolder);
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    public async Task Register_WithPrivilegedRole_ShouldReturnFailure(UserRole privilegedRole)
    {
        var request = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            UserName = "johndoe",
            Password = "Password123!",
            PhoneNumber = "1234567890",
            Role = privilegedRole
        };

        var result = await _authService.RegisterAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidRole");
    }

    [Fact]
    public async Task Register_WithInvalidRoleValue_ShouldReturnFailure()
    {
        var request = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            UserName = "johndoe",
            Password = "Password123!",
            PhoneNumber = "1234567890",
            Role = (UserRole)999
        };

        var result = await _authService.RegisterAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidRole");
    }
}