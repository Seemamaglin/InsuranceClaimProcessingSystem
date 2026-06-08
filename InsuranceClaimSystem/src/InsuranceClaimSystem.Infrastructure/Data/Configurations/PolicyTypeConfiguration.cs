using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class PolicyTypeConfiguration : IEntityTypeConfiguration<PolicyType>
{
    public void Configure(EntityTypeBuilder<PolicyType> builder)
    {
        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.TypeName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(pt => pt.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pt => pt.DefaultBenefitType)
            .HasConversion<string>();

        builder.Property(pt => pt.DefaultCoverageAmount)
            .HasColumnType("decimal(12,2)");
    }
}