using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vendors.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class VendorDocumentConfiguration : IEntityTypeConfiguration<VendorDocument>
{
    public void Configure(EntityTypeBuilder<VendorDocument> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.StoragePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(d => d.Vendor)
            .WithMany(v => v.Documents)
            .HasForeignKey(d => d.VendorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}

