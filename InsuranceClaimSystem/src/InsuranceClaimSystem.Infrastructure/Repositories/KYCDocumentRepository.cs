using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Domain.Entities;
using InsuranceClaimSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimSystem.Infrastructure.Repositories;

public class KYCDocumentRepository : Repository<KYCDocument>, IKYCDocumentRepository
{
    public KYCDocumentRepository(AppDbContext context) : base(context)
    {
    }
}
