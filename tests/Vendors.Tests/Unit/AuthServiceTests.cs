using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Identity;
using Shared.Infrastructure.Identity;
using Shared.Infrastructure.Persistence;
using Vendors.Tests.Infrastructure;

namespace Vendors.Tests.Unit;

public class AuthServiceTests
{
    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsUser_WhenPasswordCorrect()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var user = await auth.ValidateCredentialsAsync("admin@example.com", "admin123");

        user.Should().NotBeNull();
        user!.Email.Should().Be("admin@example.com");
        user.Role.Should().Be("Admin");
        user.TenantId.Should().Be(host.TenantId);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsNull_WhenPasswordWrong()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var user = await auth.ValidateCredentialsAsync("admin@example.com", "wrong-password");

        user.Should().BeNull();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsNull_WhenEmailUnknown()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var user = await auth.ValidateCredentialsAsync("nobody@example.com", "admin123");

        user.Should().BeNull();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_IsCaseInsensitiveForEmail()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var user = await auth.ValidateCredentialsAsync("Admin@Example.com", "admin123");

        user.Should().NotBeNull();
        user!.Email.Should().Be("admin@example.com");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_StoresBcryptHash_NotPlaintext()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);

        var stored = await db.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.Email == "admin@example.com");

        stored.PasswordHash.Should().StartWith("$2");
        stored.PasswordHash.Should().NotBe("admin123");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_AlwaysCallsVerify_OnUnknownEmail()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var spy = new SpyPasswordHasher(scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
        var auth = new AuthService(host.GetDb(scope), spy);

        var result = await auth.ValidateCredentialsAsync("nobody@example.com", "any-password");

        result.Should().BeNull();
        spy.VerifyCallCount.Should().Be(1);
        spy.LastHashUsed.Should().Be(AuthTimingConstants.DummyPasswordHash);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_AlwaysCallsVerify_OnWrongPassword()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);
        var spy = new SpyPasswordHasher(scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
        var auth = new AuthService(db, spy);

        var storedHash = await db.Users
            .IgnoreQueryFilters()
            .Where(u => u.Email == "admin@example.com")
            .Select(u => u.PasswordHash)
            .SingleAsync();

        var result = await auth.ValidateCredentialsAsync("admin@example.com", "wrong-password");

        result.Should().BeNull();
        spy.VerifyCallCount.Should().Be(1);
        spy.LastHashUsed.Should().Be(storedHash);
        spy.LastHashUsed.Should().NotBe(AuthTimingConstants.DummyPasswordHash);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsNull_WhenUserIsInactive()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);
        var user = await db.Users.SingleAsync(u => u.Email == "viewer@example.com");
        user.IsActive = false;
        await db.SaveChangesAsync();

        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var result = await auth.ValidateCredentialsAsync("viewer@example.com", "view123");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ReturnsNull_WhenUserIsTerminated()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == "viewer@example.com");
        user.IsDeleted = true;
        user.IsActive = false;
        await db.SaveChangesAsync();

        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var result = await auth.ValidateCredentialsAsync("viewer@example.com", "view123");

        result.Should().BeNull();
    }

    [Fact]
    public async Task IsActiveSessionAsync_IsTrue_ForActiveUser()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        (await auth.IsActiveSessionAsync("viewer@example.com")).Should().BeTrue();
        (await auth.IsActiveSessionAsync("  Viewer@Example.com  ")).Should().BeTrue();
    }

    [Fact]
    public async Task IsActiveSessionAsync_IsFalse_WhenUserIsInactive()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);
        var user = await db.Users.SingleAsync(u => u.Email == "viewer@example.com");
        user.IsActive = false;
        await db.SaveChangesAsync();

        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        (await auth.IsActiveSessionAsync("viewer@example.com")).Should().BeFalse();
    }

    [Fact]
    public async Task IsActiveSessionAsync_IsFalse_WhenUserIsTerminated()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var db = host.GetDb(scope);
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == "viewer@example.com");
        user.IsDeleted = true;
        user.IsActive = false;
        await db.SaveChangesAsync();

        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        (await auth.IsActiveSessionAsync("viewer@example.com")).Should().BeFalse();
    }

    [Fact]
    public async Task IsActiveSessionAsync_IsFalse_WhenClaimMissing()
    {
        await using var host = await VendorTestHost.CreateAsync();
        using var scope = host.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        (await auth.IsActiveSessionAsync(null)).Should().BeFalse();
        (await auth.IsActiveSessionAsync("")).Should().BeFalse();
        (await auth.IsActiveSessionAsync("nobody@example.com")).Should().BeFalse();
    }

    private sealed class SpyPasswordHasher(IPasswordHasher inner) : IPasswordHasher
    {
        public int VerifyCallCount { get; private set; }
        public string? LastHashUsed { get; private set; }

        public string Hash(string password) => inner.Hash(password);

        public bool Verify(string password, string passwordHash)
        {
            VerifyCallCount++;
            LastHashUsed = passwordHash;
            return inner.Verify(password, passwordHash);
        }
    }
}
