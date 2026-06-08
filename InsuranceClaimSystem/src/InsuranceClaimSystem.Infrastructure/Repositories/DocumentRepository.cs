using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Domain.Enums;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Application.Interfaces.Repositories;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Document>> GetByClaimIdAsync(Guid claimId)
    {
        return await _dbContext.Documents
            .AsNoTracking()
            .Where(d => d.ClaimId == claimId)
            .ToListAsync();
    }

    public async Task<int> CountPendingVerificationsAsync()
    {
        return await _dbContext.Documents
            .CountAsync(d => d.VerificationStatus == VerificationStatus.Pending);
    }
}