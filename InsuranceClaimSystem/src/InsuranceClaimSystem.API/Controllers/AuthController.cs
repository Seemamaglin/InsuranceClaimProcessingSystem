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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "EmailExists" => Conflict(result.Error),
                "UsernameExists" => Conflict(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return Ok(result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "InvalidCredentials" => Unauthorized(result.Error),
                "AccountLocked" => Unauthorized(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        if (result.IsFailure)
        {
            return Unauthorized(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Ok(new { success = result.Value });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "UserNotFound" => NotFound(result.Error),
                "InvalidToken" => Unauthorized(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return Ok(new { success = result.Value });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
    {
        var result = await _authService.VerifyEmailAsync(request);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "UserNotFound" => NotFound(result.Error),
                "InvalidCode" => Unauthorized(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return Ok(new { success = result.Value });
    }
}