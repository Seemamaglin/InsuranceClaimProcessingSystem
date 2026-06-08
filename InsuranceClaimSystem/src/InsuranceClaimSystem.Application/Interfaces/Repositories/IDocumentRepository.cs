using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Application.Interfaces.Repositories;

public interface IDocumentRepository : IRepository<Document>
{
    Task<IEnumerable<Document>> GetByClaimIdAsync(Guid claimId);
    Task<int> CountPendingVerificationsAsync();
}