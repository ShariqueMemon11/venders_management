using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vendors.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class VendorPerformanceConfiguration : IEntityTypeConfiguration<VendorPerformance>
{
    public void Configure(EntityTypeBuilder<VendorPerformance> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.EvaluationPeriod)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.QualityScore)
            .HasPrecision(5, 2);

        builder.Property(p => p.DeliveryScore)
            .HasPrecision(5, 2);

        builder.Property(p => p.ResponsivenessScore)
            .HasPrecision(5, 2);

        builder.Property(p => p.ComplianceScore)
            .HasPrecision(5, 2);

        builder.Property(p => p.AverageScore)
            .HasPrecision(5, 2);

        builder.Property(p => p.Comments)
            .HasMaxLength(1000);

        builder.Property(p => p.Evaluator)
            .HasMaxLength(100);

        builder.HasOne(p => p.Vendor)
            .WithMany(v => v.Performances)
            .HasForeignKey(p => p.VendorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

