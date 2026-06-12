using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InsuranceClaimSystem.Application.DTOs.Accounts;
using InsuranceClaimSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccount()
    {
        _logger.LogInformation("API: {Action} called", nameof(GetAccount));
        try
        {
            var userId = GetUserIdFromClaims();
            var result = await _accountService.GetAccountAsync(userId);
            if (result.IsFailure)
            {
                _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetAccount), result.Error.Code);
                return NotFound(result.Error);
            }
            _logger.LogInformation("API: {Action} succeeded", nameof(GetAccount));
            return Ok(result.Value);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("API: {Action} failed - Unauthorized", nameof(GetAccount));
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("API: {Action} failed - {Error}", nameof(GetAccount), ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(UpdateProfile));
        try
        {
            var userId = GetUserIdFromClaims();
            request.UserId = userId;
            var result = await _accountService.UpdateProfileAsync(request);
            if (result.IsFailure)
            {
                _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(UpdateProfile), result.Error.Code);
                return result.Error.Code switch
                {
                    "UserNotFound" => NotFound(result.Error),
                    "InvalidPassword" => BadRequest(result.Error),
                    _ => BadRequest(result.Error)
                };
            }
            _logger.LogInformation("API: {Action} succeeded", nameof(UpdateProfile));
            return Ok(result.Value);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("API: {Action} failed - Unauthorized", nameof(UpdateProfile));
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("API: {Action} failed - {Error}", nameof(UpdateProfile), ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(ChangePassword));
        try
        {
            var userId = GetUserIdFromClaims();
            var result = await _accountService.UpdatePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            if (result.IsFailure)
            {
                _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(ChangePassword), result.Error.Code);
                return result.Error.Code switch
                {
                    "UserNotFound" => NotFound(result.Error),
                    "InvalidPassword" => BadRequest(result.Error),
                    _ => BadRequest(result.Error)
                };
            }
            _logger.LogInformation("API: {Action} succeeded", nameof(ChangePassword));
            return Ok(new { success = result.Value });
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("API: {Action} failed - Unauthorized", nameof(ChangePassword));
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("API: {Action} failed - {Error}", nameof(ChangePassword), ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> DeactivateAccount([FromBody] DeactivateAccountRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(DeactivateAccount));
        try
        {
            var userId = GetUserIdFromClaims();
            request.UserId = userId;
            var result = await _accountService.DeactivateAccountAsync(request);
            if (result.IsFailure)
            {
                _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(DeactivateAccount), result.Error.Code);
                return result.Error.Code switch
                {
                    "UserNotFound" => NotFound(result.Error),
                    "InvalidPassword" => BadRequest(result.Error),
                    _ => BadRequest(result.Error)
                };
            }
            _logger.LogInformation("API: {Action} succeeded", nameof(DeactivateAccount));
            return Ok(new { success = result.Value });
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("API: {Action} failed - Unauthorized", nameof(DeactivateAccount));
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("API: {Action} failed - {Error}", nameof(DeactivateAccount), ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("reactivate")]
    public async Task<IActionResult> ReactivateAccount()
    {
        _logger.LogInformation("API: {Action} called", nameof(ReactivateAccount));
        try
        {
            var userId = GetUserIdFromClaims();
            var result = await _accountService.ReactivateAccountAsync(userId);
            if (result.IsFailure)
            {
                _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(ReactivateAccount), result.Error.Code);
                return NotFound(result.Error);
            }
            _logger.LogInformation("API: {Action} succeeded", nameof(ReactivateAccount));
            return Ok(new { success = result.Value });
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("API: {Action} failed - Unauthorized", nameof(ReactivateAccount));
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("API: {Action} failed - {Error}", nameof(ReactivateAccount), ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}