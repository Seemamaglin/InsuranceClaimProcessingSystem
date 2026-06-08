using System;

namespace InsuranceClaimSystem.Application.DTOs.Addresses;

public class UpdateAddressRequest
{
    public Guid AddressId { get; set; }
    public string? Type { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public bool? IsDefault { get; set; }
    public string? Landmark { get; set; }

    public UpdateAddressRequest()
    {
    }
}