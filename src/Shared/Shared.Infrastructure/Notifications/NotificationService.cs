using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Application.Notifications;
using Shared.Domain.Notifications;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public NotificationService(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = request.TenantId == Guid.Empty ? _currentUser.TenantId : request.TenantId;
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Cannot create AppNotification: TenantId is required (outbox processing has no HTTP tenant context).");
        }

        var notification = new AppNotification
        {
            TenantId = tenantId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Severity = request.Severity,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            RecipientUserId = request.RecipientUserId,
            RecipientRole = request.RecipientRole,
            ActionUrl = request.ActionUrl,
            CreatedBy = request.CreatedBy ?? "system",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
        return notification.Id;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForCurrentUserAsync(bool unreadOnly = false, int take = 50, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var role = _currentUser.Role;
        var tenantId = _currentUser.TenantId;
        var now = DateTime.UtcNow;

        var isAdmin = role == "Admin";

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId)
            .Where(n => n.ExpiresAt == null || n.ExpiresAt > now)
            .Where(n =>
                isAdmin ||
                (userId != null && n.RecipientUserId == userId) ||
                (role != null && n.RecipientRole == role));

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                Severity = n.Severity.ToString(),
                ReferenceType = n.ReferenceType,
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt,
                ActionUrl = n.ActionUrl
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var role = _currentUser.Role;
        var tenantId = _currentUser.TenantId;
        var now = DateTime.UtcNow;

        var isAdmin = role == "Admin";

        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId && !n.IsRead)
            .Where(n => n.ExpiresAt == null || n.ExpiresAt > now)
            .Where(n =>
                isAdmin ||
                (userId != null && n.RecipientUserId == userId) ||
                (role != null && n.RecipientRole == role))
            .CountAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var role = _currentUser.Role;

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == notificationId &&
                ((userId != null && n.RecipientUserId == userId) ||
                 (role != null && n.RecipientRole == role) ||
                 role == "Admin"), cancellationToken);

        if (notification == null) return;

        notification.MarkRead();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var role = _currentUser.Role;
        var tenantId = _currentUser.TenantId;

        var isAdmin = role == "Admin";

        var items = await _context.Notifications
            .Where(n => n.TenantId == tenantId && !n.IsRead)
            .Where(n =>
                isAdmin ||
                (userId != null && n.RecipientUserId == userId) ||
                (role != null && n.RecipientRole == role))
            .ToListAsync(cancellationToken);

        foreach (var item in items)
            item.MarkRead();

        if (items.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var role = _currentUser.Role;

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == notificationId &&
                ((userId != null && n.RecipientUserId == userId) ||
                 (role != null && n.RecipientRole == role) ||
                 role == "Admin"), cancellationToken);

        if (notification == null) return;

        notification.SoftDelete();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
