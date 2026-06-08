using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId);
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<AuditLog>> GetRecentAsync(int count);
}