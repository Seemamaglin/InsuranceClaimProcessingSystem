using System;

namespace InsuranceClaimSystem.Application.DTOs.Accounts;

public class UpdateProfileRequest
{
    public Guid UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Password { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }

    public UpdateProfileRequest()
    {
    }
}