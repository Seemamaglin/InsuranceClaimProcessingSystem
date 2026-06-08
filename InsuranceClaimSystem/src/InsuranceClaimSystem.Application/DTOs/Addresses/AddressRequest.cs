using System;

namespace InsuranceClaimSystem.Application.DTOs.Addresses;

public class AddressRequest
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = "India";
    public string PostalCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public string? Landmark { get; set; }

    public AddressRequest()
    {
    }
}