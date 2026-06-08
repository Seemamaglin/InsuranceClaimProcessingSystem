using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Username)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);

        builder.Property(u => u.PasswordHash).IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>();

        builder.Property(u => u.RegistrationStatus)
            .HasConversion<string>();

        builder.Property(u => u.Specialization)
            .HasConversion<string>();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasMany(u => u.Policies)
            .WithOne(p => p.PolicyHolder)
            .HasForeignKey(p => p.PolicyHolderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Claims)
            .WithOne(c => c.Claimant)
            .HasForeignKey(c => c.ClaimantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cascade = if User is deleted, all their refresh tokens are deleted too

        builder.HasMany(u => u.KYCDocuments)
            .WithOne(k => k.User)
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(u => u.Specialization);
        
    }
}