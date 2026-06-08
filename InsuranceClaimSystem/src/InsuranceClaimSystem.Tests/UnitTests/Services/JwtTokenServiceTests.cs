using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Configuration;
using InsuranceClaimSystem.Infrastructure.Services;
using InsuranceClaimSystem.Infrastructure.Services.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace InsuranceClaimSystem.Tests.UnitTests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly Mock<ILogger<JwtTokenService>> _loggerMock;
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            Secret = "this-is-a-very-long-secret-key-for-testing-purposes-only-32-chars",
            Issuer = "InsuranceClaimSystem",
            Audience = "InsuranceClaimSystemUsers",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        };

        _loggerMock = new Mock<ILogger<JwtTokenService>>();
        var optionsMock = new Mock<IOptions<JwtSettings>>();
        optionsMock.Setup(x => x.Value).Returns(_jwtSettings);

        _jwtTokenService = new JwtTokenService(optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainCorrectClaims()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "john.doe@example.com", FirstName = "John", LastName = "Doe", Role = UserRole.PolicyHolder };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        var action = () => Convert.FromBase64String(refreshToken);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateAccessToken_WithValidToken_ShouldReturnPrincipal()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "john@example.com", FirstName = "John", LastName = "Doe", Role = UserRole.ClaimReviewer };
        var token = _jwtTokenService.GenerateAccessToken(user);

        // Act
        var principal = _jwtTokenService.ValidateAccessToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void ValidateAccessToken_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange - create an expired token directly (cannot use GenerateAccessToken because DateTime.UtcNow is used for both NotBefore and Expires)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(ClaimTypes.Email, "test@example.com")
            }),
            NotBefore = DateTime.UtcNow.AddMinutes(-30),
            Expires = DateTime.UtcNow.AddMinutes(-15),
            SigningCredentials = credentials
        };
        var expiredToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        // Act
        var principal = _jwtTokenService.ValidateAccessToken(expiredToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_WithInvalidToken_ShouldReturnNull()
    {
        // Act
        var principal = _jwtTokenService.ValidateAccessToken("invalid.token.here");

        // Assert
        principal.Should().BeNull();
    }
}