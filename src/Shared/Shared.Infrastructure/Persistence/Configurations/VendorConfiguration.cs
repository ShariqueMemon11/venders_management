using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vendors.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.VendorNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.LegalName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(v => v.TradeName)
            .HasMaxLength(255);

        builder.Property(v => v.CurrencyCode)
            .HasMaxLength(3)
            .IsFixedLength();

        builder.HasOne(v => v.Tenant)
            .WithMany(t => t.Vendors)
            .HasForeignKey(v => v.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.TenantId, v.VendorNumber })
            .IsUnique();

        // Soft-delete + TenantId filters are owned by ApplicationDbContext (combined global filter).
        // A soft-delete-only filter here would be replaced — keep this as a reminder, not the source of truth.
        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}

