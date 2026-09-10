using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Authorization;
using Vendors.Api.Controllers;

namespace Vendors.Tests.Unit;

public class UsersEndpointAuthorizationTests
{
    [Fact]
    public void UsersController_RequiresPermUserManage_OnTheController()
    {
        var attr = typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>();
        attr.Should().NotBeNull();
        attr!.Policy.Should().Be("Perm.User.Manage");
    }

    [Fact]
    public void UsersController_ExposesRequiredRoutes()
    {
        var methods = typeof(UsersController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        methods.Select(m => m.Name).Should().BeEquivalentTo(
            "Create", "List", "Deactivate", "Activate", "Terminate");
    }

    [Theory]
    [InlineData("ProcurementManager")]
    [InlineData("RiskAndCompliance")]
    [InlineData("Viewer")]
    public async Task NonAdmin_IsDenied_UserManagePolicy(string role)
    {
        var authz = BuildAuthorization();
        var user = PrincipalForRole(role);

        var result = await authz.AuthorizeAsync(user, "Perm.User.Manage");

        result.Succeeded.Should().BeFalse("this is the policy that yields HTTP 403 on /api/v1/users");
    }

    [Fact]
    public async Task Admin_IsAllowed_UserManagePolicy()
    {
        var authz = BuildAuthorization();
        var user = PrincipalForRole("Admin");

        var result = await authz.AuthorizeAsync(user, "Perm.User.Manage");

        result.Succeeded.Should().BeTrue();
        AppPermissions.ForRole("Admin").Should().Contain(AppPermissions.User.Manage);
    }

    private static IAuthorizationService BuildAuthorization()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Perm.User.Manage", policy =>
                policy.RequireClaim(AppPermissions.ClaimType, AppPermissions.User.Manage));
        });
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal PrincipalForRole(string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, $"{role.ToLowerInvariant()}@example.com"),
            new(ClaimTypes.Role, role)
        };
        foreach (var perm in AppPermissions.ForRole(role))
            claims.Add(new Claim(AppPermissions.ClaimType, perm));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }
}
