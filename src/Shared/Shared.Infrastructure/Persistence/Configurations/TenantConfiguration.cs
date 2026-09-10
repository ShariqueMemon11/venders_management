using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vendors.Domain.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        // Ensure SQL Server knows Id is a uniqueidentifier (Guid)
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Identifier)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(t => t.Identifier)
            .IsUnique();
            
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

