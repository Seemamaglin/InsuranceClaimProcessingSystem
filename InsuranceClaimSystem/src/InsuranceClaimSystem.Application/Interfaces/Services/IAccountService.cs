using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Accounts;

namespace InsuranceClaimSystem.Application.Interfaces.Services;

public interface IAccountService
{
    Task<Result<AccountDto>> GetAccountAsync(Guid userId);
    Task<Result<AccountDto>> UpdateProfileAsync(UpdateProfileRequest request);
    Task<Result<bool>> UpdatePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<Result<bool>> DeactivateAccountAsync(DeactivateAccountRequest request);
    Task<Result<bool>> DeleteAccountAsync(Guid userId);
    Task<Result<bool>> ReactivateAccountAsync(Guid userId);
    Task<Result<IEnumerable<AccountDto>>> GetAllAccountsAsync();
    Task<Result<PagedResult<AccountDto>>> GetAllAccountsPagedAsync(int page, int pageSize);
}