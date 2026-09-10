using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Domain.Notifications;

namespace Shared.Infrastructure.Persistence.Configurations;

public class AppNotificationConfiguration : IEntityTypeConfiguration<AppNotification>
{
    public void Configure(EntityTypeBuilder<AppNotification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.RecipientUserId).HasMaxLength(200);
        builder.Property(n => n.RecipientRole).HasMaxLength(100);
        builder.Property(n => n.ReferenceType).HasMaxLength(100);
        builder.Property(n => n.ActionUrl).HasMaxLength(500);
        builder.Property(n => n.CreatedBy).HasMaxLength(200);

        builder.HasQueryFilter(n => !n.IsDeleted);

        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead, n.CreatedAt });
        builder.HasIndex(n => new { n.RecipientRole, n.IsRead, n.CreatedAt });
        builder.HasIndex(n => n.TenantId);
    }
}
