using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsuranceClaimSystem.Domain.Entities;

namespace InsuranceClaimSystem.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(a => a.EntityType)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(a => a.IpAddress)
               .HasMaxLength(45);

        builder.Property(a => a.OldValues)
               .HasColumnType("jsonb");

        builder.Property(a => a.NewValues)
               .HasColumnType("jsonb");

        builder.HasOne(a => a.User)
               .WithMany(u => u.AuditLogs)
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.CreatedAt);
    }
}