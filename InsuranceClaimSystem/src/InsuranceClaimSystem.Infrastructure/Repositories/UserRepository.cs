using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Common;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == role)
            .ToListAsync();
    }

    public async Task<PagedResult<User>> GetPendingRegistrationsAsync(int page, int pageSize)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Where(u => u.RegistrationStatus == RegistrationStatus.PendingApproval);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<User>.Create(items, totalCount, page, pageSize);
    }

    public async Task<int> CountFailedLoginAttemptsAsync(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.FailedLoginAttempts ?? 0;
    }

    public async Task<PagedResult<User>> GetPagedAsync(int page, int pageSize, Expression<Func<User, bool>>? predicate = null)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<User>.Create(items, totalCount, page, pageSize);
    }
}