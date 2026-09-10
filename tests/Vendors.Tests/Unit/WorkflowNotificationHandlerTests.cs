using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Platform.Workflow;
using Platform.Workflow.Enums;
using Platform.Workflow.Events;
using Platform.Workflow.ValueObjects;
using Shared.Infrastructure.Notifications;
using Vendors.Domain.Entities;
using Vendors.Tests.Infrastructure;

namespace Vendors.Tests.Unit;

/// <summary>
/// The outbox job has no HTTP user, so CurrentTenantId is Guid.Empty and tenant query
/// filters hide WorkflowInstances / Vendors. Handlers must still create notifications.
/// </summary>
public class WorkflowNotificationHandlerTests
{
    [Fact]
    public async Task WorkflowStarted_WithoutHttpTenant_CreatesNotificationFromVendor()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);
        var handler = CreateHandler(host, db);

        var vendorId = Guid.NewGuid();
        db.Vendors.Add(new Vendor
        {
            Id = vendorId,
            TenantId = host.TenantId,
            VendorNumber = "V-N-0001",
            LegalName = "Harborline Logistics Inc",
            CurrencyCode = "USD",
            CreatedBy = "test"
        });
        await db.SaveChangesAsync();

        SimulateBackgroundJob(host);

        // Old outbox payload: no TenantId on the event.
        await handler.Handle(new WorkflowStartedEvent(Guid.NewGuid(), vendorId), CancellationToken.None);

        var items = await db.Notifications.IgnoreQueryFilters().ToListAsync();
        items.Should().ContainSingle();
        items[0].TenantId.Should().Be(host.TenantId);
        items[0].RecipientRole.Should().Be("Admin");
        items[0].Title.Should().Be("Vendor submitted for approval");
        items[0].Message.Should().Contain("Harborline Logistics Inc");
    }

    [Fact]
    public async Task WorkflowTaskAssigned_WithoutHttpTenant_CreatesRoleNotification()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);
        var handler = CreateHandler(host, db);

        var vendorId = Guid.NewGuid();
        db.Vendors.Add(new Vendor
        {
            Id = vendorId,
            TenantId = host.TenantId,
            VendorNumber = "V-N-0002",
            LegalName = "Apex Semiconductor Pte. Ltd.",
            CurrencyCode = "USD",
            CreatedBy = "test"
        });

        var definition = await db.WorkflowDefinitions
            .Include(d => d.Steps)
            .SingleAsync();

        var instance = WorkflowInstance.Start(
            host.TenantId,
            definition,
            new EntityReference(WorkflowEntityType.Vendor, vendorId),
            "procurement@example.com",
            Assignment.ToRole("RiskAndCompliance"));
        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync();

        SimulateBackgroundJob(host);

        await handler.Handle(
            new WorkflowTaskAssignedEvent(Guid.NewGuid(), instance.Id, Assignment.ToRole("RiskAndCompliance")),
            CancellationToken.None);

        var items = await db.Notifications.IgnoreQueryFilters().ToListAsync();
        items.Should().ContainSingle();
        items[0].TenantId.Should().Be(host.TenantId);
        items[0].RecipientRole.Should().Be("RiskAndCompliance");
        items[0].Title.Should().Be("Workflow task assigned");
        items[0].Message.Should().Contain("Apex Semiconductor");
    }

    [Fact]
    public async Task WorkflowRejected_WithoutHttpTenant_NotifiesSubmitterAndProcurement()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);
        var handler = CreateHandler(host, db);

        var vendorId = Guid.NewGuid();
        db.Vendors.Add(new Vendor
        {
            Id = vendorId,
            TenantId = host.TenantId,
            VendorNumber = "V-N-0003",
            LegalName = "Brightwell Facilities Ltd",
            CurrencyCode = "GBP",
            CreatedBy = "test"
        });

        var definition = await db.WorkflowDefinitions
            .Include(d => d.Steps)
            .SingleAsync();

        var instance = WorkflowInstance.Start(
            host.TenantId,
            definition,
            new EntityReference(WorkflowEntityType.Vendor, vendorId),
            "procurement@example.com",
            Assignment.ToRole("RiskAndCompliance"));
        db.WorkflowInstances.Add(instance);
        await db.SaveChangesAsync();

        SimulateBackgroundJob(host);

        await handler.Handle(new WorkflowRejectedEvent(instance.Id, vendorId), CancellationToken.None);

        var items = await db.Notifications.IgnoreQueryFilters().OrderBy(n => n.Title).ToListAsync();
        items.Should().HaveCount(2);
        items.Should().OnlyContain(n => n.TenantId == host.TenantId);
        items.Should().Contain(n => n.RecipientUserId == "procurement@example.com");
        items.Should().Contain(n => n.RecipientRole == "ProcurementManager");
    }

    private static WorkflowNotificationHandlers CreateHandler(VendorTestHost host, Shared.Infrastructure.Persistence.ApplicationDbContext db)
    {
        var notifications = new NotificationService(db, host.CurrentUser);
        return new WorkflowNotificationHandlers(db, notifications, NullLogger<WorkflowNotificationHandlers>.Instance);
    }

    private static void SimulateBackgroundJob(VendorTestHost host)
    {
        host.CurrentUser.UserId = null;
        host.CurrentUser.Role = null;
        host.CurrentUser.TenantId = Guid.Empty;
    }
}
