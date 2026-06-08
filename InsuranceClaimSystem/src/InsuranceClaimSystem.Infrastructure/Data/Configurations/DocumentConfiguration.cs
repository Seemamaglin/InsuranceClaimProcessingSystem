using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName)
               .HasMaxLength(255)
               .IsRequired();

        builder.Property(d => d.FileUrl)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(d => d.MimeType)
               .HasMaxLength(100);

        builder.Property(d => d.VerificationStatus)
               .HasConversion<string>();

        builder.Property(d => d.DocumentType)
               .HasConversion<string>();

        builder.Property(d => d.FileSizeInBytes);

        builder.HasOne(d => d.Claim)
               .WithMany(c => c.Documents)
               .HasForeignKey(d => d.ClaimId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.UploadedByUser)
               .WithMany()
               .HasForeignKey(d => d.UploadedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.VerifiedByUser)
               .WithMany()
               .HasForeignKey(d => d.VerifiedByUserId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasIndex(d => d.ClaimId);
    }
}