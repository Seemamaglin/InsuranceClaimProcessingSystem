using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClaimNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(c => c.ClaimNumber).IsUnique();

        builder.Property(c => c.ClaimedAmount)
            .HasColumnType("decimal(12,2)");

        builder.Property(c => c.ApprovedAmount)
            .HasColumnType("decimal(12,2)");

        builder.Property(c => c.FinalPayableAmount)
            .HasColumnType("decimal(12,2)");
        // Calculated: MIN(ClaimedAmount, SubLimit) × (1 - CoPayPercent/100) - Deductible

        builder.Property(c => c.DeductibleAmount)
            .HasColumnType("decimal(12,2)");

        builder.Property(c => c.CoPayPercentage)
            .HasColumnType("decimal(5,2)");

        builder.Property(c => c.Status)
            .HasConversion<string>();

        builder.Property(c => c.ClaimantType)
            .HasConversion<string>();

        builder.Property(c => c.RowVersion)
            .IsRowVersion(); 
       
        builder.HasOne(c => c.Policy)
            .WithMany(p => p.Claims)
            .HasForeignKey(c => c.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Claimant)
            .WithMany(u => u.Claims)
            .HasForeignKey(c => c.ClaimantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.AssignedReviewer)
            .WithMany()
            .HasForeignKey(c => c.AssignedReviewerId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        
        builder.HasOne(c => c.Nominee)
            .WithMany()
            .HasForeignKey(c => c.NomineeId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        
        builder.HasOne(c => c.ClaimType)
            .WithMany()
            .HasForeignKey(c => c.ClaimTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Documents)
            .WithOne(d => d.Claim)
            .HasForeignKey(d => d.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ClaimPayments)
            .WithOne(p => p.Claim)
            .HasForeignKey(p => p.ClaimId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.WorkflowHistories)
            .WithOne(w => w.Claim)
            .HasForeignKey(w => w.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ClaimNotes)
            .WithOne(n => n.Claim)
            .HasForeignKey(n => n.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ThirdPartyClaimants)
            .WithOne(t => t.Claim)
            .HasForeignKey(t => t.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.PolicyId);
        builder.HasIndex(c => c.Status);
        
        builder.HasIndex(c => c.AssignedReviewerId);
        builder.HasIndex(c => c.CreatedAt);
    }
}