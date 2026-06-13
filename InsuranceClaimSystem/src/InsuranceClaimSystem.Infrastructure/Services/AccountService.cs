using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Application.DTOs.Accounts;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
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
        _logger.LogInformation("Getting account for user {UserId}", userId);
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return Result<AccountDto>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            _logger.LogInformation("Account retrieved successfully for user {UserId}", userId);
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
        _logger.LogInformation("Updating profile for user {UserId}", request.UserId);
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for profile update", request.UserId);
                return Result<AccountDto>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            var updateResult = UpdateUserProfile(user, request);
            if (updateResult.IsFailure)
            {
                return Result<AccountDto>.Failure(updateResult.Error);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Profile updated successfully for user {UserId}", request.UserId);
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
        _logger.LogInformation("Updating password for user {UserId}", userId);
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for password update", userId);
                return Result<bool>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Invalid current password for user {UserId}", userId);
                return Result<bool>.Failure(
                    Error.Validation("InvalidPassword", "Current password is incorrect."));
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);
            user.IsFirstLogin = false;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password updated successfully for user {UserId}", userId);
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
        _logger.LogInformation("Deactivating account for user {UserId}", request.UserId);
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for deactivation", request.UserId);
                return Result<bool>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for account deactivation {UserId}", request.UserId);
                return Result<bool>.Failure(
                    Error.Validation("InvalidPassword", "Password is incorrect."));
            }

            user.IsActive = false;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Account deactivated successfully for user {UserId}", request.UserId);
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
        _logger.LogInformation("Deleting account for user {UserId}", userId);
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for deletion", userId);
                return Result<bool>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            user.IsActive = false;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Account deleted successfully for user {UserId}", userId);
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
        _logger.LogInformation("Reactivating account for user {UserId}", userId);
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for reactivation", userId);
                return Result<bool>.Failure(
                    Error.NotFound("UserNotFound", "User not found."));
            }

            user.IsActive = true;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Account reactivated successfully for user {UserId}", userId);
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
        _logger.LogInformation("Getting all accounts");
        try
        {
            var users = await _userRepository.GetAllAsync();
            var accountDtos = new List<AccountDto>();
            foreach (var user in users)
            {
                accountDtos.Add(MapToAccountDto(user));
            }

            _logger.LogInformation("Retrieved {Count} accounts", accountDtos.Count);
            return Result<IEnumerable<AccountDto>>.Success(accountDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all accounts");
            return Result<IEnumerable<AccountDto>>.Failure(
                Error.Validation("GetAccountsFailed", "An error occurred while retrieving accounts."));
        }
    }

    public async Task<Result<PagedResult<AccountDto>>> GetAllAccountsPagedAsync(int page, int pageSize, UserRole? role = null)
    {
        _logger.LogInformation("Getting paged accounts - page {Page}, size {PageSize}, role {Role}", page, pageSize, role);
        try
        {
            var predicate = role.HasValue
                ? (Expression<Func<User, bool>>)(x => x.Role == role.Value)
                : x => true;

            var pagedResult = await _userRepository.GetPagedAsync(page, pageSize, predicate);

            var accountDtos = pagedResult.Items
                .Select(MapToAccountDto)
                .ToList();

            var result = PagedResult<AccountDto>.Create(accountDtos, pagedResult.TotalCount, page, pageSize);
            _logger.LogInformation("Retrieved paged accounts - total {Total}, page {Page}", pagedResult.TotalCount, page);
            return Result<PagedResult<AccountDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged accounts");
            return Result<PagedResult<AccountDto>>.Failure(
                Error.Validation("GetAccountsFailed", "An error occurred while retrieving accounts."));
        }
    }

    private static AccountDto MapToAccountDto(User user)
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

    private static Result<bool> UpdateUserProfile(User user, UpdateProfileRequest request)
    {
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
                return Result<bool>.Failure(
                    Error.Validation("InvalidPassword", "Current password is incorrect."));
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
        }

        return Result<bool>.Success(true);
    }
}