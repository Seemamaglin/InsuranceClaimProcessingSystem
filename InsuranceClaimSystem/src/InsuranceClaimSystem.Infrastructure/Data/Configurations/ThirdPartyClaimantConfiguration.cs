using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class ThirdPartyClaimantConfiguration : IEntityTypeConfiguration<ThirdPartyClaimant>
{
    public void Configure(EntityTypeBuilder<ThirdPartyClaimant> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.ContactPhone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.ContactEmail)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.DamageDescription)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(t => t.EstimatedDamageAmount)
            .HasColumnType("decimal(12,2)");

        builder.Property(t => t.PoliceReportNumber)
            .HasMaxLength(50);

        builder.HasOne(t => t.Claim)
            .WithMany(c => c.ThirdPartyClaimants)
            .HasForeignKey(t => t.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.ClaimId);
    }
}