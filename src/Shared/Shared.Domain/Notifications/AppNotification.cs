using Shared.Domain.Common;

namespace Shared.Domain.Notifications;

public class AppNotification : EntityBase, ITenantEntity
{
    public Guid TenantId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }

    /// <summary>Specific user email/id. Null when targeted by role only.</summary>
    public string? RecipientUserId { get; set; }

    /// <summary>Role name (e.g. RiskAndCompliance). Null when targeted to a user only.</summary>
    public string? RecipientRole { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ActionUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
