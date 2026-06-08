using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.TokenId);

        builder.Property(r => r.Token)
               .HasMaxLength(500)
               .IsRequired();
        // Stored as SHA-256 hash — raw token is only in the httpOnly cookie

        builder.Property(r => r.RevokedReason)
               .HasMaxLength(100);

        builder.HasIndex(r => r.Token);
        // Every refresh request does: WHERE Token = '{hash}'
        // Without this index, each refresh scans the entire table

        builder.HasIndex(r => r.UserId);

        builder.HasOne(r => r.User)
               .WithMany(u => u.RefreshTokens)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}