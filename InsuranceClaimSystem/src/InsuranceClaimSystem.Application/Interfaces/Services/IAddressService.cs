using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Addresses;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface IAddressService
{
    Task<Result<AddressDto>> CreateAddressAsync(AddressRequest request);
    Task<Result<bool>> UpdateAddressAsync(UpdateAddressRequest request);
    Task<Result<bool>> DeleteAddressAsync(Guid addressId);
    Task<Result<AddressDto>> GetAddressByIdAsync(Guid addressId);
    Task<Result<IEnumerable<AddressDto>>> GetAddressesByUserAsync(Guid userId);
    Task<Result<bool>> SetDefaultAddressAsync(Guid addressId, Guid userId);
}