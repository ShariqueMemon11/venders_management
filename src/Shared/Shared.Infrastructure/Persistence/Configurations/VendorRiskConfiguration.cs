using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vendors.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class VendorRiskConfiguration : IEntityTypeConfiguration<VendorRisk>
{
    public void Configure(EntityTypeBuilder<VendorRisk> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RiskCategory)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.RiskDescription)
            .HasMaxLength(1000);

        builder.Property(r => r.MitigationPlan)
            .HasMaxLength(1000);

        builder.HasOne(r => r.Vendor)
            .WithMany(v => v.Risks)
            .HasForeignKey(r => r.VendorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

