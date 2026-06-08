using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class ClaimPaymentConfiguration : IEntityTypeConfiguration<ClaimPayment>
{
    public void Configure(EntityTypeBuilder<ClaimPayment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
               .HasColumnType("decimal(12,2)");

        builder.Property(p => p.IdempotencyKey)
               .IsRequired();

        builder.HasIndex(p => p.IdempotencyKey).IsUnique();
        // Critical for Stripe — if a payment request is retried,
        // the same IdempotencyKey prevents a duplicate charge

        builder.Property(p => p.PaymentStatus)
               .HasConversion<string>();

        builder.Property(p => p.RecipientType)
               .HasConversion<string>();

        builder.Property(p => p.RecipientName).HasMaxLength(200);
        builder.Property(p => p.RecipientAccountNumber).HasMaxLength(50);
        builder.Property(p => p.RecipientBankName).HasMaxLength(100);

        builder.HasOne(p => p.Claim)
               .WithMany(c => c.ClaimPayments)
               .HasForeignKey(p => p.ClaimId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}