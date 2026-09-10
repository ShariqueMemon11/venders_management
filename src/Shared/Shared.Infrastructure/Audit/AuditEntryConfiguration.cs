using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Audit;

namespace Shared.Infrastructure.Audit;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditTrails");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId).HasMaxLength(100);
        builder.Property(e => e.ActionType).HasMaxLength(50);
        builder.Property(e => e.TableName).HasMaxLength(100);
    }
}