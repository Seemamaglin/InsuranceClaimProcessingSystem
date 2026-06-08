using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class PolicyBenefitRuleConfiguration : IEntityTypeConfiguration<PolicyBenefitRule>
{
    public void Configure(EntityTypeBuilder<PolicyBenefitRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.CoPayPercent)
               .HasColumnType("decimal(5,2)");

        builder.Property(r => r.MaxClaimablePercent)
               .HasColumnType("decimal(5,2)");

        builder.Property(r => r.SubLimitAmount)
               .HasColumnType("decimal(12,2)");

        builder.Property(r => r.DeductibleAmount)
               .HasColumnType("decimal(12,2)");

        builder.Property(r => r.SubLimitDescription)
               .HasMaxLength(255);

        builder.HasOne(r => r.PolicyType)
               .WithMany()
               .HasForeignKey(r => r.PolicyTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ClaimType)
               .WithMany()
               .HasForeignKey(r => r.ClaimTypeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}