using System;

namespace InsuranceClaimSystem.Application.DTOs.Accounts;

public class DeactivateAccountRequest
{
    public Guid UserId { get; set; }
    public string Password { get; set; } = string.Empty;

    public DeactivateAccountRequest()
    {
    }
}