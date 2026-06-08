using System;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.DTOs.Accounts;

public class AccountDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? LastLoginAt { get; set; }
    public UserRole Role { get; set; }
    public Specialization? Specialization { get; set; }
    public RegistrationStatus RegistrationStatus { get; set; }
    public bool IsActive { get; set; }
    public bool IsFirstLogin { get; set; }
    public DateTime CreatedAt { get; set; }

    public AccountDto()
    {
    }
}