using System.Linq.Expressions;
using InsuranceClaimSystem.Application.Common;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
    Task<PagedResult<User>> GetPendingRegistrationsAsync(int page, int pageSize);
    Task<int> CountFailedLoginAttemptsAsync(Guid userId);
}