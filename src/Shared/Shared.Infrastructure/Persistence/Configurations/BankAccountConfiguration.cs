using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vendors.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BankName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.AccountName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.AccountNumber)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(b => b.Iban)
            .HasMaxLength(512);

        builder.Property(b => b.SwiftBic)
            .HasMaxLength(20);

        builder.Property(b => b.CurrencyCode)
            .HasMaxLength(3)
            .IsFixedLength();

        builder.HasOne(b => b.Vendor)
            .WithMany(v => v.BankAccounts)
            .HasForeignKey(b => b.VendorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}

