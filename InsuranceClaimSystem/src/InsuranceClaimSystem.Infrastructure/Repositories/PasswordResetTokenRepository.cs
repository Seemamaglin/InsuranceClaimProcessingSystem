using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _dbContext;

    public PasswordResetTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == tokenHash);
    }

    public async Task<PasswordResetToken?> GetActiveTokenByUserIdAsync(Guid userId)
    {
        return await _dbContext.PasswordResetTokens
            .Where(t => t.UserId == userId && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(PasswordResetToken token)
    {
        await _dbContext.PasswordResetTokens.AddAsync(token);
    }

    public Task UpdateAsync(PasswordResetToken token)
    {
        _dbContext.PasswordResetTokens.Update(token);
        return Task.CompletedTask;
    }
}