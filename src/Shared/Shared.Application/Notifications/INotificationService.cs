using Shared.Domain.Notifications;

namespace Shared.Application.Notifications;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ActionUrl { get; set; }
}

public class CreateNotificationRequest
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? RecipientUserId { get; set; }
    public string? RecipientRole { get; set; }
    public string? ActionUrl { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public interface INotificationService
{
    Task<Guid> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> GetForCurrentUserAsync(bool unreadOnly = false, int take = 50, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Guid notificationId, CancellationToken cancellationToken = default);
}
