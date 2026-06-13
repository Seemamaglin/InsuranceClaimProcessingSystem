using System;
using System.Threading.Tasks;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountService _accountService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAccountService accountService,
        ILogger<AdminController> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _accountService = accountService;
        _logger = logger;
    }

    [HttpGet("registrations/pending")]
    public async Task<IActionResult> GetPendingRegistrations([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("API: {Action} called", nameof(GetPendingRegistrations));
        try
        {
            var result = await _userRepository.GetPendingRegistrationsAsync(page, pageSize);
            _logger.LogInformation("API: {Action} succeeded", nameof(GetPendingRegistrations));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("API: {Action} failed - {Error}", nameof(GetPendingRegistrations), ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("registrations/{userId}/approve")]
    public async Task<IActionResult> ApproveRegistration(Guid userId)
    {
        _logger.LogInformation("API: {Action} called", nameof(ApproveRegistration));
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("API: {Action} failed - UserNotFound", nameof(ApproveRegistration));
                return NotFound(new { error = "User not found." });
            }

            if (user.RegistrationStatus != RegistrationStatus.PendingApproval)
            {
                _logger.LogWarning("API: {Action} failed - InvalidStatus", nameof(ApproveRegistration));
                return BadRequest(new { error = "User is not pending approval." });
            }

            user.RegistrationStatus = RegistrationStatus.Approved;
            user.IsActive = true;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("API: {Action} succeeded", nameof(ApproveRegistration));
            return Ok(new { success = true, message = "Registration approved successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("API: {Action} failed - {Error}", nameof(ApproveRegistration), ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("registrations/{userId}/reject")]
    public async Task<IActionResult> RejectRegistration(Guid userId, [FromBody] RejectRegistrationRequest request)
    {
        _logger.LogInformation("API: {Action} called", nameof(RejectRegistration));
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("API: {Action} failed - UserNotFound", nameof(RejectRegistration));
                return NotFound(new { error = "User not found." });
            }

            if (user.RegistrationStatus != RegistrationStatus.PendingApproval)
            {
                _logger.LogWarning("API: {Action} failed - InvalidStatus", nameof(RejectRegistration));
                return BadRequest(new { error = "User is not pending approval." });
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                _logger.LogWarning("API: {Action} failed - MissingReason", nameof(RejectRegistration));
                return BadRequest(new { error = "Rejection reason is required." });
            }

            user.RegistrationStatus = RegistrationStatus.Rejected;
            user.RegistrationRejectionReason = request.Reason;
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("API: {Action} succeeded", nameof(RejectRegistration));
            return Ok(new { success = true, message = "Registration rejected." });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("API: {Action} failed - {Error}", nameof(RejectRegistration), ex.Message);
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] UserRole? role = null)
    {
        _logger.LogInformation("API: {Action} called with role {Role}", nameof(GetAllUsers), role);
        var result = await _accountService.GetAllAccountsPagedAsync(page, pageSize, role);
        if (result.IsFailure)
        {
            _logger.LogWarning("API: {Action} failed - {ErrorCode}", nameof(GetAllUsers), result.Error.Code);
            return BadRequest(result.Error);
        }
        _logger.LogInformation("API: {Action} succeeded", nameof(GetAllUsers));
        return Ok(result.Value);
    }
}

public class RejectRegistrationRequest
{
    public string Reason { get; set; } = string.Empty;
}