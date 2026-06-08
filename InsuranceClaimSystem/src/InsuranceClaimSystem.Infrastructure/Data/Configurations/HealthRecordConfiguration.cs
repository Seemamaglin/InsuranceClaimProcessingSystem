using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class HealthRecordConfiguration : IEntityTypeConfiguration<HealthRecord>
{
    public void Configure(EntityTypeBuilder<HealthRecord> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.HeightCm)
            .HasColumnType("decimal(6,2)");

        builder.Property(h => h.WeightKg)
            .HasColumnType("decimal(6,2)");

        builder.HasOne(h => h.Policy)
            .WithOne(p => p.HealthRecord)
            .HasForeignKey<HealthRecord>(h => h.PolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.PolicyHolder)
            .WithMany()
            .HasForeignKey(h => h.PolicyHolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.PolicyId).IsUnique();
        builder.HasIndex(h => h.PolicyHolderId);
    }
}