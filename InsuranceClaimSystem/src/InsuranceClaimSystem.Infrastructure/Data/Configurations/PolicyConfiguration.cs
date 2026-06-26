using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PolicyNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.PolicyNumber).IsUnique();

        builder.Property(p => p.PremiumAmount)
            .HasColumnType("decimal(12,2)");
        
        builder.Property(p => p.CoverageAmount)
            .HasColumnType("decimal(12,2)");

        builder.Property(p => p.RemainingCoverageAmount)
            .HasColumnType("decimal(12,2)");

        builder.UseXminAsConcurrencyToken();
        builder.HasOne(p => p.PolicyType)
            .WithMany()
            .HasForeignKey(p => p.PolicyTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(p => p.PolicyHolder)
               .WithMany(u => u.Policies)
               .HasForeignKey(p => p.PolicyHolderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Claims)
               .WithOne(c => c.Policy)
               .HasForeignKey(c => c.PolicyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Nominees)
               .WithOne(n => n.Policy)
               .HasForeignKey(n => n.PolicyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PolicyPayments)
               .WithOne(pp => pp.Policy)
               .HasForeignKey(pp => pp.PolicyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.HealthRecord)
            .WithOne(h => h.Policy)
            .HasForeignKey<HealthRecord>(h => h.PolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.PolicyHolderId);

        builder.HasIndex(p => p.Status);
    }
}