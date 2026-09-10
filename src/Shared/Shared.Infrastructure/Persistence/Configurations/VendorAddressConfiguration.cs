using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vendors.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class VendorAddressConfiguration : IEntityTypeConfiguration<VendorAddress>
{
    public void Configure(EntityTypeBuilder<VendorAddress> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AddressLine1)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.AddressLine2)
            .HasMaxLength(255);

        builder.Property(a => a.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.StateProvince)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Country)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.PostalCode)
            .HasMaxLength(20);

        builder.HasOne(a => a.Vendor)
            .WithMany(v => v.Addresses)
            .HasForeignKey(a => a.VendorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

