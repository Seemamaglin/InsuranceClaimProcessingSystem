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

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
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
        try
        {
            var userId = GetUserIdFromClaims();
            var result = await _accountService.GetAccountAsync(userId);
            if (result.IsFailure)
            {
                return NotFound(result.Error);
            }
            return Ok(result.Value);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            request.UserId = userId;
            var result = await _accountService.UpdateProfileAsync(request);
            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    "UserNotFound" => NotFound(result.Error),
                    "InvalidPassword" => BadRequest(result.Error),
                    _ => BadRequest(result.Error)
                };
            }
            return Ok(result.Value);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            var result = await _accountService.UpdatePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    "UserNotFound" => NotFound(result.Error),
                    "InvalidPassword" => BadRequest(result.Error),
                    _ => BadRequest(result.Error)
                };
            }
            return Ok(new { success = result.Value });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("deactivate")]
    public async Task<IActionResult> DeactivateAccount([FromBody] DeactivateAccountRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            request.UserId = userId;
            var result = await _accountService.DeactivateAccountAsync(request);
            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    "UserNotFound" => NotFound(result.Error),
                    "InvalidPassword" => BadRequest(result.Error),
                    _ => BadRequest(result.Error)
                };
            }
            return Ok(new { success = result.Value });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("reactivate")]
    public async Task<IActionResult> ReactivateAccount()
    {
        try
        {
            var userId = GetUserIdFromClaims();
            var result = await _accountService.ReactivateAccountAsync(userId);
            if (result.IsFailure)
            {
                return NotFound(result.Error);
            }
            return Ok(new { success = result.Value });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}