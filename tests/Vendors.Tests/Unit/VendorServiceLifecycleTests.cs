using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vendors.Application.Vendors.Dtos;
using Vendors.Domain.Enums;
using Vendors.Tests.Infrastructure;

namespace Vendors.Tests.Unit;

/// <summary>
/// VendorService status / lifecycle rules that must hold without the workflow engine.
/// Create always Draft; Update never mutates Status; Terminate soft-sets Terminated.
/// </summary>
public class VendorServiceLifecycleTests
{
    [Fact]
    public async Task CreateAsync_AlwaysPersistsDraft_EvenIfClientSendsActiveStatus()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);
        var db = host.GetDb(scope);

        var result = await sut.CreateAsync(new VendorDto
        {
            LegalName = "Lifecycle Create Co",
            CurrencyCode = "USD",
            Status = VendorStatus.Active // client attempt to escalate — must be ignored
        }, host.TenantId);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);

        var saved = await db.Vendors.SingleAsync(v => v.Id == result.Data);
        saved.Status.Should().Be(VendorStatus.Draft);
        saved.LegalName.Should().Be("Lifecycle Create Co");
        saved.TenantId.Should().Be(host.TenantId);
        saved.VendorNumber.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateAsync_UsesProvidedVendorNumber_WhenPresent()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);

        var result = await sut.CreateAsync(new VendorDto
        {
            LegalName = "Numbered Co",
            CurrencyCode = "USD",
            VendorNumber = "V-FIXED-0001"
        }, host.TenantId);

        result.Success.Should().BeTrue();
        using var scope2 = host.CreateScope();
        var db = host.GetDb(scope2);
        var saved = await db.Vendors.SingleAsync(v => v.Id == result.Data);
        saved.VendorNumber.Should().Be("V-FIXED-0001");
    }

    [Fact]
    public async Task UpdateAsync_ChangesProfileFields_ButDoesNotChangeStatus()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);

        var created = await sut.CreateAsync(new VendorDto
        {
            LegalName = "Before Update",
            CurrencyCode = "USD",
            TaxRegistrationNumber = "TAX-OLD"
        }, host.TenantId);

        // Force a non-Draft status in DB to prove Update does not overwrite Status from DTO.
        using (var mutateScope = host.CreateScope())
        {
            var db = host.GetDb(mutateScope);
            var entity = await db.Vendors.SingleAsync(v => v.Id == created.Data);
            entity.Status = VendorStatus.PendingReview;
            await db.SaveChangesAsync();
        }

        var update = await sut.UpdateAsync(new VendorDto
        {
            Id = created.Data,
            LegalName = "After Update",
            TradeName = "After DBA",
            CurrencyCode = "EUR",
            TaxRegistrationNumber = "TAX-NEW",
            Website = "https://example.com",
            Status = VendorStatus.Active // must be ignored
        }, host.TenantId);

        update.Success.Should().BeTrue();

        using var assertScope = host.CreateScope();
        var assertDb = host.GetDb(assertScope);
        var saved = await assertDb.Vendors.SingleAsync(v => v.Id == created.Data);
        saved.LegalName.Should().Be("After Update");
        saved.TradeName.Should().Be("After DBA");
        saved.CurrencyCode.Should().Be("EUR");
        saved.TaxRegistrationNumber.Should().Be("TAX-NEW");
        saved.Website.Should().Be("https://example.com");
        saved.Status.Should().Be(VendorStatus.PendingReview, "status is workflow-owned and Update must not touch it");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailure_WhenVendorMissingOrWrongTenant()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);

        var missing = await sut.UpdateAsync(new VendorDto
        {
            Id = Guid.NewGuid(),
            LegalName = "Nope",
            CurrencyCode = "USD"
        }, host.TenantId);

        missing.Success.Should().BeFalse();
        missing.Message.Should().Contain("not found");

        var created = await sut.CreateAsync(new VendorDto
        {
            LegalName = "Other Tenant Guard",
            CurrencyCode = "USD"
        }, host.TenantId);

        var wrongTenant = await sut.UpdateAsync(new VendorDto
        {
            Id = created.Data,
            LegalName = "Hijack",
            CurrencyCode = "USD"
        }, Guid.NewGuid());

        wrongTenant.Success.Should().BeFalse();
        wrongTenant.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task TerminateAsync_SetsStatusTerminated_AndKeepsRecord()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);

        var created = await sut.CreateAsync(new VendorDto
        {
            LegalName = "To Terminate",
            CurrencyCode = "USD"
        }, host.TenantId);

        var terminated = await sut.TerminateAsync(created.Data, host.TenantId);
        terminated.Success.Should().BeTrue();

        using var assertScope = host.CreateScope();
        var db = host.GetDb(assertScope);
        var saved = await db.Vendors.SingleAsync(v => v.Id == created.Data);
        saved.Status.Should().Be(VendorStatus.Terminated);
        saved.IsDeleted.Should().BeFalse("terminate is soft status change, not hard/soft-delete");
    }

    [Fact]
    public async Task TerminateAsync_ReturnsFailure_WhenNotFound()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);

        var result = await sut.TerminateAsync(Guid.NewGuid(), host.TenantId);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToTerminate_NotHardDelete()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = host.GetVendorService(scope);

        var created = await sut.CreateAsync(new VendorDto
        {
            LegalName = "Delete Alias",
            CurrencyCode = "USD"
        }, host.TenantId);

        var deleted = await sut.DeleteAsync(created.Data, host.TenantId);
        deleted.Success.Should().BeTrue();

        using var assertScope = host.CreateScope();
        var db = host.GetDb(assertScope);
        var saved = await db.Vendors.IgnoreQueryFilters().SingleAsync(v => v.Id == created.Data);
        saved.Status.Should().Be(VendorStatus.Terminated);
        saved.IsDeleted.Should().BeFalse();
    }
}
