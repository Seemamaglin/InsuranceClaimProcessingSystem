using System.Linq.Expressions;
using FluentAssertions;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Accounts;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class AccountServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<AccountService>> _loggerMock;
    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AccountService>>();

        _accountService = new AccountService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAccountAsync_WithExistingUser_ShouldReturnAccountDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Username = "johndoe",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Role = UserRole.PolicyHolder,
            IsActive = true,
            IsFirstLogin = false,
            RegistrationStatus = RegistrationStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _accountService.GetAccountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(userId);
        result.Value.FullName.Should().Be("John Doe");
        result.Value.Email.Should().Be("john.doe@example.com");
        result.Value.Username.Should().Be("johndoe");
    }

    [Fact]
    public async Task GetAccountAsync_WithNonExistingUser_ShouldReturnNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _accountService.GetAccountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_ShouldUpdateAndReturnAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Username = "johndoe",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Role = UserRole.PolicyHolder,
            IsActive = true,
            IsFirstLogin = false,
            RegistrationStatus = RegistrationStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };

        var request = new UpdateProfileRequest
        {
            UserId = userId,
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "9876543210"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _accountService.UpdateProfileAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FullName.Should().Be("Jane Smith");
        result.Value.PhoneNumber.Should().Be("9876543210");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!", 12);
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Username = "johndoe",
            PasswordHash = passwordHash,
            Role = UserRole.PolicyHolder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var request = new UpdateProfileRequest
        {
            UserId = userId,
            Password = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _accountService.UpdateProfileAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidPassword");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithNonExistingUser_ShouldReturnNotFound()
    {
        // Arrange
        var request = new UpdateProfileRequest
        {
            UserId = Guid.NewGuid(),
            FirstName = "Jane"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act
        var result = await _accountService.UpdateProfileAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithCorrectPassword_ShouldUpdatePassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "CurrentPassword123!";
        var newPassword = "NewPassword123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(currentPassword, 12);
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Username = "johndoe",
            PasswordHash = passwordHash,
            IsFirstLogin = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _accountService.UpdatePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash).Should().BeTrue();
        user.IsFirstLogin.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithIncorrectPassword_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var correctPassword = "CorrectPassword123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword, 12);
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _accountService.UpdatePasswordAsync(userId, "WrongPassword", "NewPassword123!");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidPassword");
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithNonExistingUser_ShouldReturnNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _accountService.UpdatePasswordAsync(userId, "password", "newpassword");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task DeactivateAccountAsync_WithCorrectPassword_ShouldDeactivate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var password = "Password123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, 12);
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var request = new DeactivateAccountRequest
        {
            UserId = userId,
            Password = password
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _accountService.DeactivateAccountAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAccountAsync_WithIncorrectPassword_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var correctPassword = "CorrectPassword123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword, 12);
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var request = new DeactivateAccountRequest
        {
            UserId = userId,
            Password = "WrongPassword"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _accountService.DeactivateAccountAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("InvalidPassword");
    }

    [Fact]
    public async Task ReactivateAccountAsync_WithExistingUser_ShouldReactivate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _accountService.ReactivateAccountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAccountsAsync_ShouldReturnAllAccounts()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Username = "johndoe",
                Role = UserRole.PolicyHolder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Username = "janesmith",
                Role = UserRole.ClaimReviewer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _userRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _accountService.GetAllAccountsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAccountsPaged_WithRoleFilter_ShouldFilterByRole()
    {
        var users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com",
                Username = "admin",
                Role = UserRole.Admin
            }
        };

        _userRepositoryMock.Setup(x => x.GetPagedAsync(1, 10, It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(PagedResult<User>.Create(users, users.Count, 1, 10));

        var result = await _accountService.GetAllAccountsPagedAsync(1, 10, UserRole.Admin);

        result.IsSuccess.Should().BeTrue();
        _userRepositoryMock.Verify(x => x.GetPagedAsync(1, 10, It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAccountsPaged_WithoutRoleFilter_ShouldReturnAll()
    {
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@example.com", Username = "john", Role = UserRole.PolicyHolder },
            new User { Id = Guid.NewGuid(), FirstName = "Admin", LastName = "User", Email = "admin@example.com", Username = "admin", Role = UserRole.Admin }
        };

        _userRepositoryMock.Setup(x => x.GetPagedAsync(1, 10, It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(PagedResult<User>.Create(users, users.Count, 1, 10));

        var result = await _accountService.GetAllAccountsPagedAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }
}