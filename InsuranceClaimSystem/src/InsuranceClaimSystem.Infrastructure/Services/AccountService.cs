using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Accounts;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace InsuranceClaimSystem.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<AccountService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AccountDto>> GetAccountAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result<AccountDto>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            return Result<AccountDto>.Success(MapToAccountDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account for {UserId}", userId);
            return Result<AccountDto>.Failure(
                Error.Validation("GetAccountFailed", "An error occurred while retrieving the account."));
        }
    }

    public async Task<Result<AccountDto>> UpdateProfileAsync(UpdateProfileRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return Result<AccountDto>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName;

            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName;

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber;

            if (request.DateOfBirth.HasValue)
                user.DateOfBirth = request.DateOfBirth.Value;

            if (!string.IsNullOrWhiteSpace(request.Password) && !string.IsNullOrWhiteSpace(request.NewPassword))
            {
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Result<AccountDto>.Failure(
                        Error.Validation("InvalidPassword", "Current password is incorrect."));
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
            }

            await _unitOfWork.SaveChangesAsync();

            return Result<AccountDto>.Success(MapToAccountDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for {UserId}", request.UserId);
            return Result<AccountDto>.Failure(
                Error.Validation("UpdateProfileFailed", "An error occurred while updating the profile."));
        }
    }

    public async Task<Result<bool>> UpdatePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return Result<bool>.Failure(
                    Error.Validation("InvalidPassword", "Current password is incorrect."));
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);
            user.IsFirstLogin = false;

            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating password for {UserId}", userId);
            return Result<bool>.Failure(
                Error.Validation("UpdatePasswordFailed", "An error occurred while updating the password."));
        }
    }

    public async Task<Result<bool>> DeactivateAccountAsync(DeactivateAccountRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Result<bool>.Failure(
                    Error.Validation("InvalidPassword", "Password is incorrect."));
            }

            user.IsActive = false;

            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating account for {UserId}", request.UserId);
            return Result<bool>.Failure(
                Error.Validation("DeactivateFailed", "An error occurred while deactivating the account."));
        }
    }

    public async Task<Result<bool>> DeleteAccountAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            user.IsActive = false;

            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account for {UserId}", userId);
            return Result<bool>.Failure(
                Error.Validation("DeleteFailed", "An error occurred while deleting the account."));
        }
    }

    public async Task<Result<bool>> ReactivateAccountAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            user.IsActive = true;

            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating account for {UserId}", userId);
            return Result<bool>.Failure(
                Error.Validation("ReactivateFailed", "An error occurred while reactivating the account."));
        }
    }

    public async Task<Result<IEnumerable<AccountDto>>> GetAllAccountsAsync()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var accountDtos = new List<AccountDto>();
            foreach (var user in users)
            {
                accountDtos.Add(MapToAccountDto(user));
            }

            return Result<IEnumerable<AccountDto>>.Success(accountDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all accounts");
            return Result<IEnumerable<AccountDto>>.Failure(
                Error.Validation("GetAccountsFailed", "An error occurred while retrieving accounts."));
        }
    }

    public async Task<Result<PagedResult<AccountDto>>> GetAllAccountsPagedAsync(int page, int pageSize)
    {
        try
        {
            var allUsers = await _userRepository.GetAllAsync();
            var totalCount = allUsers.Count();
            var pagedUsers = allUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var accountDtos = new List<AccountDto>();
            foreach (var user in pagedUsers)
            {
                accountDtos.Add(MapToAccountDto(user));
            }

            var pagedResult = PagedResult<AccountDto>.Create(accountDtos, totalCount, page, pageSize);
            return Result<PagedResult<AccountDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged accounts");
            return Result<PagedResult<AccountDto>>.Failure(
                Error.Validation("GetAccountsFailed", "An error occurred while retrieving accounts."));
        }
    }

    private static AccountDto MapToAccountDto(dynamic user)
    {
        return new AccountDto
        {
            Id = user.Id,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email,
            Username = user.Username,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            LastLoginAt = user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            Role = user.Role,
            Specialization = user.Specialization,
            RegistrationStatus = user.RegistrationStatus,
            IsActive = user.IsActive,
            IsFirstLogin = user.IsFirstLogin,
            CreatedAt = user.CreatedAt
        };
    }
}