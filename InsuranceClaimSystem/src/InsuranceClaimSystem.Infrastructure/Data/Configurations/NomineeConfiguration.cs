using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class NomineeConfiguration : IEntityTypeConfiguration<Nominee>
{
    public void Configure(EntityTypeBuilder<Nominee> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Relationship)
            .HasConversion<string>();

        builder.Property(n => n.ContactPhone)
            .HasMaxLength(20);

        builder.Property(n => n.ContactEmail)
            .HasMaxLength(200);

        builder.Property(n => n.AadhaarKeyReference)
            .HasMaxLength(500);

        builder.Property(n => n.AadhaarMasked)
            .HasMaxLength(20);

        builder.Property(n => n.SharePercentage)
            .HasColumnType("decimal(5,2)");

        builder.HasOne(n => n.Policy)
            .WithMany(p => p.Nominees)
            .HasForeignKey(n => n.PolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.PolicyHolder)
            .WithMany()
            .HasForeignKey(n => n.PolicyHolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.PolicyId);
        builder.HasIndex(n => n.PolicyHolderId);
    }
}