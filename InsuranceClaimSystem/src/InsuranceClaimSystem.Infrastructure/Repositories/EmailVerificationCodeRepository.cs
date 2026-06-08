using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class EmailVerificationCodeRepository : IEmailVerificationCodeRepository
{
    private readonly AppDbContext _dbContext;

    public EmailVerificationCodeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmailVerificationCode?> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.EmailVerificationCodes
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<EmailVerificationCode?> GetActiveCodeByUserIdAsync(Guid userId)
    {
        return await _dbContext.EmailVerificationCodes
            .Where(c => c.UserId == userId && !c.IsUsed && c.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(EmailVerificationCode code)
    {
        await _dbContext.EmailVerificationCodes.AddAsync(code);
    }

    public Task UpdateAsync(EmailVerificationCode code)
    {
        _dbContext.EmailVerificationCodes.Update(code);
        return Task.CompletedTask;
    }
}