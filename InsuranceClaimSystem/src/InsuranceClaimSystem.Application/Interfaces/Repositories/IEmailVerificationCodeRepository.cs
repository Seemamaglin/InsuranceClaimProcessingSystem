using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IEmailVerificationCodeRepository
{
    Task<EmailVerificationCode?> GetByUserIdAsync(Guid userId);
    Task<EmailVerificationCode?> GetActiveCodeByUserIdAsync(Guid userId);
    Task AddAsync(EmailVerificationCode code);
    Task UpdateAsync(EmailVerificationCode code);
}