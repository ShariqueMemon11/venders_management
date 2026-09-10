using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vendors.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class VendorComplianceConfiguration : IEntityTypeConfiguration<VendorCompliance>
{
    public void Configure(EntityTypeBuilder<VendorCompliance> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.RequirementName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.CheckedBy)
            .HasMaxLength(100);

        builder.Property(c => c.Comments)
            .HasMaxLength(1000);

        builder.HasOne(c => c.Vendor)
            .WithMany(v => v.Compliances)
            .HasForeignKey(c => c.VendorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

