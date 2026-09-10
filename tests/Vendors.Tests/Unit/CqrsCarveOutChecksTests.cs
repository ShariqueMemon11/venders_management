using System.Text.RegularExpressions;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vendors.Application.Vendors.Commands.TerminateVendor;
using Vendors.Application.Vendors.Commands.UpdateVendor;
using Vendors.Application.Vendors.Dtos;
using Vendors.Application.Vendors.Queries.GetVendorById;
using Vendors.Application.Vendors.Queries.GetVendorDirectory;
using Vendors.Domain.Enums;
using Vendors.Tests.Infrastructure;

namespace Vendors.Tests.Unit;

/// <summary>
/// Spot-checks for Batch 3e CQRS carve-outs (business rules + filter inheritance).
/// </summary>
public class CqrsCarveOutChecksTests
{
    private static readonly Regex AutoVendorNumber =
        new(@"^V-\d{8}-[A-F0-9]{4}$", RegexOptions.CultureInvariant);

    [Fact]
    public async Task CreateAsync_AutoVendorNumber_MatchesPreCarveOutScheme()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);
        var db = host.GetDb(scope);

        var result = await sut.CreateAsync(new VendorDto
        {
            LegalName = "Number Scheme Co",
            CurrencyCode = "USD"
        }, host.TenantId);

        result.Success.Should().BeTrue();
        var saved = await db.Vendors.SingleAsync(v => v.Id == result.Data);
        saved.VendorNumber.Should().MatchRegex(AutoVendorNumber);
        saved.VendorNumber.Should().StartWith($"V-{DateTime.UtcNow:yyyyMMdd}-");
    }

    [Fact]
    public async Task GetVendorDirectoryQuery_ExcludesSoftDeletedVendors()
    {
        await using var host = await VendorTestHost.CreateAsync();

        Guid keepId;
        Guid softDeletedId;

        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var keep = await sut.CreateAsync(new VendorDto
            {
                LegalName = "Still Visible Co",
                CurrencyCode = "USD"
            }, host.TenantId);
            var gone = await sut.CreateAsync(new VendorDto
            {
                LegalName = "Soft Deleted Co",
                CurrencyCode = "USD"
            }, host.TenantId);

            keep.Success.Should().BeTrue();
            gone.Success.Should().BeTrue();
            keepId = keep.Data;
            softDeletedId = gone.Data;
        }

        // Soft-delete via EF Deleted → IsDeleted interceptor (Batch 2a), not Terminate (status only).
        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var entity = await db.Vendors.SingleAsync(v => v.Id == softDeletedId);
            db.Vendors.Remove(entity);
            await db.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Prove the row still exists under IgnoreQueryFilters (soft, not hard delete).
            var tombstone = await db.Vendors
                .IgnoreQueryFilters()
                .SingleAsync(v => v.Id == softDeletedId);
            tombstone.IsDeleted.Should().BeTrue();

            var directory = await mediator.Send(
                new GetVendorDirectoryQuery(host.TenantId, Page: 1, PageSize: 50));

            directory.Success.Should().BeTrue();
            directory.Data!.Items.Should().Contain(v => v.Id == keepId);
            directory.Data.Items.Should().NotContain(v => v.Id == softDeletedId);
            directory.Data.Items.Should().NotContain(v => v.LegalName == "Soft Deleted Co");
        }
    }

    [Fact]
    public async Task UpdateVendorCommand_DoesNotChangeStatus()
    {
        await using var host = await VendorTestHost.CreateAsync();
        Guid vendorId;

        using (var scope = host.CreateScope())
        {
            var sut = host.GetVendorService(scope);
            var created = await sut.CreateAsync(new VendorDto
            {
                LegalName = "Update Status Guard",
                CurrencyCode = "USD"
            }, host.TenantId);
            created.Success.Should().BeTrue();
            vendorId = created.Data;
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var entity = await db.Vendors.SingleAsync(v => v.Id == vendorId);
            entity.Status = VendorStatus.PendingReview;
            await db.SaveChangesAsync();
        }

        using (var scope = host.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new UpdateVendorCommand(new VendorDto
            {
                Id = vendorId,
                LegalName = "Updated Name",
                CurrencyCode = "EUR",
                Status = VendorStatus.Active
            }, host.TenantId));

            result.Success.Should().BeTrue();
        }

        using (var scope = host.CreateScope())
        {
            var db = host.GetDb(scope);
            var saved = await db.Vendors.SingleAsync(v => v.Id == vendorId);
            saved.LegalName.Should().Be("Updated Name");
            saved.CurrencyCode.Should().Be("EUR");
            saved.Status.Should().Be(VendorStatus.PendingReview);
        }
    }

    [Fact]
    public async Task TerminateVendorCommand_SetsTerminated_WithoutIsDeleted()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var created = await sut.CreateAsync(new VendorDto
        {
            LegalName = "Terminate Slice Co",
            CurrencyCode = "USD"
        }, host.TenantId);

        var terminated = await mediator.Send(
            new TerminateVendorCommand(created.Data, host.TenantId));
        terminated.Success.Should().BeTrue();

        var db = host.GetDb(scope);
        var saved = await db.Vendors.SingleAsync(v => v.Id == created.Data);
        saved.Status.Should().Be(VendorStatus.Terminated);
        saved.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetVendorByIdQuery_ReturnsVendor_AndFailsForWrongTenant()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var created = await sut.CreateAsync(new VendorDto
        {
            LegalName = "ById Slice Co",
            CurrencyCode = "USD",
            TaxRegistrationNumber = "TAX-BYID"
        }, host.TenantId);

        var found = await mediator.Send(new GetVendorByIdQuery(created.Data, host.TenantId));
        found.Success.Should().BeTrue();
        found.Data!.LegalName.Should().Be("ById Slice Co");
        found.Data.TaxRegistrationNumber.Should().Be("TAX-BYID");
        found.Data.Status.Should().Be(VendorStatus.Draft);

        var wrongTenant = await mediator.Send(
            new GetVendorByIdQuery(created.Data, Guid.NewGuid()));
        wrongTenant.Success.Should().BeFalse();
        wrongTenant.Message.Should().Contain("not found");
    }
}
