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

    [HttpPost("users/create-staff")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
    {
        _logger.LogInformation("API: {Action} called for role {Role}", nameof(CreateStaff), request.Role);
        try
        {
            // Validate role
            var allowedRoles = new[] { UserRole.ClaimReviewer, UserRole.ClaimsManager, UserRole.FinanceOfficer };
            if (!Enum.IsDefined(typeof(UserRole), request.Role) || !allowedRoles.Contains(request.Role))
            {
                _logger.LogWarning("API: {Action} failed - InvalidRole", nameof(CreateStaff));
                return BadRequest(new { error = "Invalid staff role. Allowed: ClaimReviewer(3), ClaimsManager(2), FinanceOfficer(4)." });
            }

            // Check email uniqueness
            var existingByEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingByEmail != null)
            {
                _logger.LogWarning("API: {Action} failed - EmailExists", nameof(CreateStaff));
                return Conflict(new { error = "A user with this email already exists." });
            }

            // Check username uniqueness
            var existingByUsername = await _userRepository.GetByUsernameAsync(request.UserName);
            if (existingByUsername != null)
            {
                _logger.LogWarning("API: {Action} failed - UsernameExists", nameof(CreateStaff));
                return Conflict(new { error = "A user with this username already exists." });
            }

            var staffUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Username = request.UserName,
                Email = request.Email,
                EmailVerifiedAt = DateTime.UtcNow,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
                PhoneNumber = request.PhoneNumber,
                Role = request.Role,
                Specialization = request.Specialization,
                RegistrationStatus = RegistrationStatus.Approved,
                IsActive = true,
                IsFirstLogin = true,
                FailedLoginAttempts = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(staffUser);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("API: {Action} succeeded - UserId {UserId}", nameof(CreateStaff), staffUser.Id);
            return StatusCode(201, new
            {
                userId = staffUser.Id,
                email = staffUser.Email,
                role = staffUser.Role,
                message = "Staff account created successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating staff account");
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }
}

public class RejectRegistrationRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class CreateStaffRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Specialization? Specialization { get; set; }
}