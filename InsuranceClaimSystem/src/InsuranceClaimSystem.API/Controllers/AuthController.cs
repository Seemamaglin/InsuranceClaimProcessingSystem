using System.Threading.Tasks;
using InsuranceClaimSystem.Application.DTOs.Auth;
using InsuranceClaimSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(Register));
        var result = await _authService.RegisterAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(Register), result.Error.Code);
            return result.Error.Code switch
            {
                "EmailExists" => Conflict(result.Error),
                "UsernameExists" => Conflict(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(Register));
        return Ok(result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(Login));
        var result = await _authService.LoginAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(Login), result.Error.Code);
            return result.Error.Code switch
            {
                "InvalidCredentials" => Unauthorized(result.Error),
                "AccountLocked" => Unauthorized(result.Error),
                "AccountPending" => Unauthorized(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(Login));
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(RefreshToken));
        var result = await _authService.RefreshTokenAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(RefreshToken), result.Error.Code);
            return Unauthorized(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(RefreshToken));
        return Ok(result.Value);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(ForgotPassword));
        var result = await _authService.ForgotPasswordAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(ForgotPassword), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(ForgotPassword));
        return Ok(new { success = result.Value });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(ResetPassword));
        var result = await _authService.ResetPasswordAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(ResetPassword), result.Error.Code);
            return result.Error.Code switch
            {
                "UserNotFound" => NotFound(result.Error),
                "InvalidToken" => Unauthorized(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(ResetPassword));
        return Ok(new { success = result.Value });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(VerifyEmail));
        var result = await _authService.VerifyEmailAsync(request);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(VerifyEmail), result.Error.Code);
            return result.Error.Code switch
            {
                "UserNotFound" => NotFound(result.Error),
                "InvalidCode" => Unauthorized(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(VerifyEmail));
        return Ok(new { success = result.Value });
    }
}