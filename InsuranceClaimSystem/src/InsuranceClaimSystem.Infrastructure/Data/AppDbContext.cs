using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimSystem.Domain.Common;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data;

public class AppDbContext : DbContext
{
    //constructor
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options) 
    {
    }
    //database provider to use and the connection string
    // base(options) send those settings to the parent DbContext

    public DbSet<User> Users { get; set; }
    public DbSet<Policy> Policies { get; set; }
    public DbSet<Claim> Claims { get; set; }
    public DbSet<ClaimNote> ClaimNotes { get; set; }
    public DbSet<ClaimPayment> ClaimPayments { get; set; }
    public DbSet<ClaimType> ClaimTypes { get; set; }
    public DbSet<ClaimWorkflowHistory> WorkFlowHistory { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Nominee> Nominees { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PolicyBenefitRule> PolicyBenefitRules { get; set; }
    public DbSet<PolicyType> PolicyTypes { get; set; }
    public DbSet<ThirdPartyClaimant> ThirdPartyClaimants { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<HealthRecord> HealthRecords { get; set; }
    public DbSet<KYCDocument> KYCDocuments { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<PolicyPayment> PolicyPayments { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<InsuranceClaimSystem.Domain.Common.DomainEvent>();

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
        
        modelBuilder.Entity<PasswordResetToken>()
            .HasKey(p => p.ResetTokenId);
        
        modelBuilder.Entity<EmailVerificationCode>()
            .HasKey(e => e.Id);
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Property(e => e.CreatedAt).IsModified = false;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }
            return await base.SaveChangesAsync(cancellationToken);
        }

        private static LambdaExpression BuildSoftDeleteFilter(Type entityType)
        {
            var param = Expression.Parameter(entityType, "entity");
            var body = Expression.Not(
                Expression.Property(
                    Expression.Convert(param, typeof(BaseEntity)),
                    nameof(BaseEntity.IsDeleted)));
            return Expression.Lambda(body, param);
        }
}