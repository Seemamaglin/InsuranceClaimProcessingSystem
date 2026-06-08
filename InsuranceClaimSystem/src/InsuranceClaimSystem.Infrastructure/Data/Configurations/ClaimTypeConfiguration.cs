using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class ClaimTypeConfiguration : IEntityTypeConfiguration<ClaimType>
{
    public void Configure(EntityTypeBuilder<ClaimType> builder)
    {
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.TypeName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(ct => ct.PolicyType)
            .WithMany()
            .HasForeignKey(ct => ct.PolicyTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ct => ct.PolicyTypeId);

        builder.Ignore(ct => ct.RequiredDocuments);
    }
}