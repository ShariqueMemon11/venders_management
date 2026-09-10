using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vendors.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class VendorContractConfiguration : IEntityTypeConfiguration<VendorContract>
{
    public void Configure(EntityTypeBuilder<VendorContract> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ContractNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.ContractValue)
            .HasPrecision(18, 2);

        builder.Property(c => c.CurrencyCode)
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(c => c.PaymentTerms)
            .HasMaxLength(100);

        builder.Property(c => c.SlaBrief)
            .HasMaxLength(1000);

        builder.HasOne(c => c.Vendor)
            .WithMany(v => v.Contracts)
            .HasForeignKey(c => c.VendorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

