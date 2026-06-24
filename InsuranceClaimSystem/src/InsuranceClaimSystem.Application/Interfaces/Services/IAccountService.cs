using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Accounts;
using InsuranceClaimSystem.Domain.Enums;

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
    Task<Result<PagedResult<AccountDto>>> GetAllAccountsPagedAsync(int page, int pageSize, UserRole? role = null);
    
    Task<Result<bool>> SubmitKycAsync(Guid userId, List<IFormFile> documents);
    Task<Result<bool>> ApproveRegistrationAsync(Guid targetUserId, Guid adminId, bool isApproved, string? rejectionReason);
    Task<Result<IEnumerable<AccountDto>>> GetPendingApprovalsAsync();
}