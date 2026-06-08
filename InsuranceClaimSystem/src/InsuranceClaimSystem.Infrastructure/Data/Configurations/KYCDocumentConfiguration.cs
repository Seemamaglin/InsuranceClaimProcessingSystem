using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class KYCDocumentConfiguration : IEntityTypeConfiguration<KYCDocument>
{
    public void Configure(EntityTypeBuilder<KYCDocument> builder)
    {
        builder.HasKey(k => k.Id);

        builder.Property(k => k.FilePath)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(k => k.FileName).HasMaxLength(255);

        builder.Property(k => k.DocumentType)
               .HasConversion<string>();

        builder.Property(k => k.VerificationStatus)
               .HasConversion<string>();

        builder.HasOne(k => k.User)
               .WithMany(u => u.KYCDocuments)
               .HasForeignKey(k => k.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(k => k.VerifiedByAdmin)
               .WithMany()
               .HasForeignKey(k => k.VerifiedByAdminId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasIndex(k => k.UserId);
    }
}