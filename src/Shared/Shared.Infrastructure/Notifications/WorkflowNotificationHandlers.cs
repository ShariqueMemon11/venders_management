using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Workflow.Enums;
using Platform.Workflow.Events;
using Shared.Application.Notifications;
using Shared.Domain.Notifications;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.Notifications;

public class WorkflowNotificationHandlers :
    INotificationHandler<WorkflowStartedEvent>,
    INotificationHandler<WorkflowTaskAssignedEvent>,
    INotificationHandler<WorkflowCompletedEvent>,
    INotificationHandler<WorkflowRejectedEvent>
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<WorkflowNotificationHandlers> _logger;

    public WorkflowNotificationHandlers(
        ApplicationDbContext db,
        INotificationService notifications,
        ILogger<WorkflowNotificationHandlers> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task Handle(WorkflowStartedEvent notification, CancellationToken cancellationToken)
    {
        var ctx = await ResolveAsync(notification.WorkflowInstanceId, notification.EntityId, notification.TenantId, cancellationToken);
        if (ctx == null) return;

        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            TenantId = ctx.TenantId,
            Title = "Vendor submitted for approval",
            Message = $"{ctx.VendorName} was submitted and the approval workflow has started.",
            Type = NotificationType.VendorSubmitted,
            Severity = NotificationSeverity.Info,
            ReferenceType = "Vendor",
            ReferenceId = ctx.VendorId,
            RecipientRole = "Admin",
            ActionUrl = $"/vendors/{ctx.VendorId}",
            CreatedBy = "workflow-engine"
        }, cancellationToken);

        _logger.LogInformation("Notification created for WorkflowStarted {InstanceId}", notification.WorkflowInstanceId);
    }

    public async Task Handle(WorkflowTaskAssignedEvent notification, CancellationToken cancellationToken)
    {
        var ctx = await ResolveAsync(notification.WorkflowInstanceId, entityId: null, notification.TenantId, cancellationToken);
        if (ctx == null) return;

        var request = new CreateNotificationRequest
        {
            TenantId = ctx.TenantId,
            Title = "Workflow task assigned",
            Message = $"Action required: review {ctx.VendorName}.",
            Type = NotificationType.WorkflowAssigned,
            Severity = NotificationSeverity.Warning,
            ReferenceType = "Vendor",
            ReferenceId = ctx.VendorId,
            ActionUrl = $"/vendors/{ctx.VendorId}",
            CreatedBy = "workflow-engine"
        };

        if (notification.Assignment.Strategy == AssignmentStrategy.User)
            request.RecipientUserId = notification.Assignment.Value;
        else
            request.RecipientRole = notification.Assignment.Value;

        await _notifications.CreateAsync(request, cancellationToken);
        _logger.LogInformation("Notification created for WorkflowTaskAssigned {TaskId}", notification.TaskId);
    }

    public async Task Handle(WorkflowCompletedEvent notification, CancellationToken cancellationToken)
    {
        var ctx = await ResolveAsync(notification.WorkflowInstanceId, notification.EntityId, notification.TenantId, cancellationToken);
        if (ctx == null) return;

        if (!string.IsNullOrWhiteSpace(ctx.CreatedBy))
        {
            await _notifications.CreateAsync(new CreateNotificationRequest
            {
                TenantId = ctx.TenantId,
                Title = "Vendor approved",
                Message = $"{ctx.VendorName} has been fully approved and is now Active.",
                Type = NotificationType.VendorApproved,
                Severity = NotificationSeverity.Success,
                ReferenceType = "Vendor",
                ReferenceId = ctx.VendorId,
                RecipientUserId = ctx.CreatedBy,
                ActionUrl = $"/vendors/{ctx.VendorId}",
                CreatedBy = "workflow-engine"
            }, cancellationToken);
        }

        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            TenantId = ctx.TenantId,
            Title = "Workflow completed",
            Message = $"Approval workflow for {ctx.VendorName} completed successfully.",
            Type = NotificationType.WorkflowCompleted,
            Severity = NotificationSeverity.Success,
            ReferenceType = "Vendor",
            ReferenceId = ctx.VendorId,
            RecipientRole = "ProcurementManager",
            ActionUrl = $"/vendors/{ctx.VendorId}",
            CreatedBy = "workflow-engine"
        }, cancellationToken);
    }

    public async Task Handle(WorkflowRejectedEvent notification, CancellationToken cancellationToken)
    {
        var ctx = await ResolveAsync(notification.WorkflowInstanceId, notification.EntityId, notification.TenantId, cancellationToken);
        if (ctx == null) return;

        if (!string.IsNullOrWhiteSpace(ctx.CreatedBy))
        {
            await _notifications.CreateAsync(new CreateNotificationRequest
            {
                TenantId = ctx.TenantId,
                Title = "Vendor rejected",
                Message = $"{ctx.VendorName} was rejected during approval. Please review and resubmit if needed.",
                Type = NotificationType.VendorRejected,
                Severity = NotificationSeverity.Critical,
                ReferenceType = "Vendor",
                ReferenceId = ctx.VendorId,
                RecipientUserId = ctx.CreatedBy,
                ActionUrl = $"/vendors/{ctx.VendorId}",
                CreatedBy = "workflow-engine"
            }, cancellationToken);
        }

        await _notifications.CreateAsync(new CreateNotificationRequest
        {
            TenantId = ctx.TenantId,
            Title = "Vendor rejected",
            Message = $"{ctx.VendorName} was rejected in the approval workflow.",
            Type = NotificationType.VendorRejected,
            Severity = NotificationSeverity.Warning,
            ReferenceType = "Vendor",
            ReferenceId = ctx.VendorId,
            RecipientRole = "ProcurementManager",
            ActionUrl = $"/vendors/{ctx.VendorId}",
            CreatedBy = "workflow-engine"
        }, cancellationToken);
    }

    /// <summary>
    /// Outbox processing has no HTTP user, so global tenant query filters would hide
    /// WorkflowInstances / Vendors (TenantId == Guid.Empty). Always bypass filters here.
    /// </summary>
    private async Task<NotificationContext?> ResolveAsync(
        Guid workflowInstanceId,
        Guid? entityId,
        Guid eventTenantId,
        CancellationToken cancellationToken)
    {
        var instance = await _db.WorkflowInstances
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workflowInstanceId, cancellationToken);

        var vendorId = entityId is { } id && id != Guid.Empty
            ? id
            : instance?.TargetEntity.Id ?? Guid.Empty;

        var tenantId = eventTenantId != Guid.Empty
            ? eventTenantId
            : instance?.TenantId ?? Guid.Empty;

        var vendorName = "Vendor";
        if (vendorId != Guid.Empty)
        {
            var vendor = await _db.Vendors
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(v => v.Id == vendorId)
                .Select(v => new { v.LegalName, v.TenantId })
                .FirstOrDefaultAsync(cancellationToken);

            if (vendor != null)
            {
                vendorName = string.IsNullOrWhiteSpace(vendor.LegalName) ? "Vendor" : vendor.LegalName;
                if (tenantId == Guid.Empty)
                    tenantId = vendor.TenantId;
            }
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning(
                "Skipping workflow notification: no TenantId for instance {InstanceId} / vendor {VendorId}.",
                workflowInstanceId,
                vendorId);
            return null;
        }

        if (vendorId == Guid.Empty)
        {
            _logger.LogWarning(
                "Skipping workflow notification: no vendor id for instance {InstanceId}.",
                workflowInstanceId);
            return null;
        }

        return new NotificationContext(tenantId, vendorId, vendorName, instance?.CreatedBy);
    }

    private sealed record NotificationContext(Guid TenantId, Guid VendorId, string VendorName, string? CreatedBy);
}
