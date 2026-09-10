using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Common.Interfaces;
using Shared.Application.Identity;
using Shared.Domain.Identity;
using Vendors.Domain.Entities;
using Vendors.Tests.Infrastructure;

namespace Vendors.Tests.Unit;

public class UserManagementServiceTests
{
    private static CreateUserRequest NewUser(
        string email = "new.user@example.com",
        string role = "Viewer",
        string name = "New User",
        string password = "password1") =>
        new()
        {
            DisplayName = name,
            Email = email,
            Role = role,
            Password = password
        };

    [Fact]
    public async Task Create_HashesPassword_AndReturnsId()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        host.CurrentUser.As("admin@example.com", "Admin", host.TenantId);
        var sut = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
        var db = host.GetDb(scope);

        var result = await sut.CreateAsync(NewUser(), host.TenantId);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBe(Guid.Empty);

        var stored = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == result.Data);
        stored.Email.Should().Be("new.user@example.com");
        stored.Role.Should().Be("Viewer");
        stored.TenantId.Should().Be(host.TenantId);
        stored.PasswordHash.Should().StartWith("$2");
        stored.PasswordHash.Should().NotBe("password1");
        stored.IsActive.Should().BeTrue();
        stored.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Create_NormalizesEmail_CaseInsensitive()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        var result = await sut.CreateAsync(NewUser(email: "  New.User@Example.COM "), host.TenantId);

        result.Success.Should().BeTrue();
        var stored = await host.GetDb(scope).Users.SingleAsync(u => u.Id == result.Data);
        stored.Email.Should().Be("new.user@example.com");
    }

    [Fact]
    public async Task Create_RejectsDuplicateEmail()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        (await sut.CreateAsync(NewUser(email: "dup@example.com"), host.TenantId)).Success.Should().BeTrue();
        var dup = await sut.CreateAsync(NewUser(email: "DUP@example.com"), host.TenantId);

        dup.Success.Should().BeFalse();
        dup.Message.Should().Be("Email is already in use.");
    }

    [Fact]
    public async Task Create_RejectsAdminRole()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        var result = await sut.CreateAsync(NewUser(role: "Admin"), host.TenantId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ProcurementManager");
        (await host.GetDb(scope).Users.CountAsync(u => u.Role == "Admin")).Should().Be(1);
    }

    [Theory]
    [InlineData("ProcurementManager")]
    [InlineData("RiskAndCompliance")]
    [InlineData("Viewer")]
    public async Task Create_AllowsAssignableRoles(string role)
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        var result = await sut.CreateAsync(NewUser(email: $"{role.ToLowerInvariant()}@corp.test", role: role), host.TenantId);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task List_IsScopedToTenant_AndOmitsPasswordHash()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);
        var otherTenant = Guid.Parse("00000000-0000-0000-0000-000000000099");
        db.Tenants.Add(new Tenant
        {
            Id = otherTenant,
            Name = "Other Co",
            Identifier = "other",
            IsActive = true,
            CreatedBy = "test"
        });
        db.Users.Add(new ApplicationUser
        {
            Email = "other@tenant.test",
            DisplayName = "Other",
            Role = "Viewer",
            TenantId = otherTenant,
            PasswordHash = "not-a-real-hash",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var sut = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
        var list = await sut.ListAsync(host.TenantId);

        list.Success.Should().BeTrue();
        list.Data.Should().NotBeNull();
        list.Data!.Should().NotContain(u => u.Email == "other@tenant.test");
        list.Data.Should().Contain(u => u.Email == "admin@example.com");
        var json = System.Text.Json.JsonSerializer.Serialize(list.Data);
        json.Should().NotContain("PasswordHash");
        json.Should().NotContain("passwordHash");
        json.Should().NotContain("$2");
    }

    [Fact]
    public async Task Deactivate_Activate_RoundTrip()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
        var created = await sut.CreateAsync(NewUser(), host.TenantId);

        var off = await sut.SetActiveAsync(created.Data, false, host.TenantId, "admin@example.com");
        off.Success.Should().BeTrue();
        (await host.GetDb(scope).Users.SingleAsync(u => u.Id == created.Data)).IsActive.Should().BeFalse();

        var on = await sut.SetActiveAsync(created.Data, true, host.TenantId, "admin@example.com");
        on.Success.Should().BeTrue();
        (await host.GetDb(scope).Users.SingleAsync(u => u.Id == created.Data)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Terminate_IsPermanent()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
        var created = await sut.CreateAsync(NewUser(), host.TenantId);

        (await sut.TerminateAsync(created.Data, host.TenantId, "admin@example.com")).Success.Should().BeTrue();
        var stored = await host.GetDb(scope).Users.IgnoreQueryFilters().SingleAsync(u => u.Id == created.Data);
        stored.IsDeleted.Should().BeTrue();
        stored.IsActive.Should().BeFalse();

        var reactivate = await sut.SetActiveAsync(created.Data, true, host.TenantId, "admin@example.com");
        reactivate.Success.Should().BeFalse();
        reactivate.Message.Should().Be("Terminated users cannot be reactivated.");

        var listed = await sut.ListAsync(host.TenantId);
        listed.Data.Should().Contain(u => u.Id == created.Data && u.IsDeleted && u.Status == "Terminated");
    }

    [Fact]
    public async Task Deactivate_AndTerminate_BlockSelfLockout()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);
        var adminId = await db.Users.Where(u => u.Email == "admin@example.com").Select(u => u.Id).SingleAsync();
        var sut = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        var deactivate = await sut.SetActiveAsync(adminId, false, host.TenantId, "admin@example.com");
        deactivate.Success.Should().BeFalse();
        deactivate.Message.Should().Be("You cannot deactivate your own account.");

        var terminate = await sut.TerminateAsync(adminId, host.TenantId, "admin@example.com");
        terminate.Success.Should().BeFalse();
        terminate.Message.Should().Be("You cannot terminate your own account.");

        var still = await db.Users.SingleAsync(u => u.Id == adminId);
        still.IsActive.Should().BeTrue();
        still.IsDeleted.Should().BeFalse();
    }
}
