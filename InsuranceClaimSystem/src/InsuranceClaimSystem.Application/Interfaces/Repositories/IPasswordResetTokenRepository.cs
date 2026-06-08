using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);
    Task<PasswordResetToken?> GetActiveTokenByUserIdAsync(Guid userId);
    Task AddAsync(PasswordResetToken token);
    Task UpdateAsync(PasswordResetToken token);
}