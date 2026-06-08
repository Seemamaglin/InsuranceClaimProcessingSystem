using System;
using System.Threading.Tasks;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceClaimSystem.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminController(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("registrations/pending")]
    public async Task<IActionResult> GetPendingRegistrations([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _userRepository.GetPendingRegistrationsAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("registrations/{userId}/approve")]
    public async Task<IActionResult> ApproveRegistration(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

            if (user.RegistrationStatus != RegistrationStatus.PendingApproval)
            {
                return BadRequest(new { error = "User is not pending approval." });
            }

            user.RegistrationStatus = RegistrationStatus.Approved;
            user.IsActive = true;
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { success = true, message = "Registration approved successfully." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpPost("registrations/{userId}/reject")]
    public async Task<IActionResult> RejectRegistration(Guid userId, [FromBody] RejectRegistrationRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

            if (user.RegistrationStatus != RegistrationStatus.PendingApproval)
            {
                return BadRequest(new { error = "User is not pending approval." });
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { error = "Rejection reason is required." });
            }

            user.RegistrationStatus = RegistrationStatus.Rejected;
            user.RegistrationRejectionReason = request.Reason;
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { success = true, message = "Registration rejected." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var allUsers = await _userRepository.GetAllAsync();
            var totalCount = allUsers.Count();
            var pagedUsers = allUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var result = PagedResult<User>.Create(pagedUsers, totalCount, page, pageSize);
            return Ok(result);
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "An unexpected error occurred." });
        }
    }
}

public class RejectRegistrationRequest
{
    public string Reason { get; set; } = string.Empty;
}