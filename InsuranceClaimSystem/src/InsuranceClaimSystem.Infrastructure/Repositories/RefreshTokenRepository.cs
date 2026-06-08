using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public RefreshTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task<RefreshToken?> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(RefreshToken token)
    {
        await _dbContext.RefreshTokens.AddAsync(token);
    }

    public Task UpdateAsync(RefreshToken token)
    {
        _dbContext.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task RevokeFamilyAsync(string token)
    {
        var refreshToken = await GetByTokenAsync(token);
        if (refreshToken == null) return;

        var familyTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == refreshToken.UserId && t.CreatedAt >= refreshToken.CreatedAt)
            .ToListAsync();

        foreach (var t in familyTokens)
        {
            t.IsRevoked = true;
        }
    }
}