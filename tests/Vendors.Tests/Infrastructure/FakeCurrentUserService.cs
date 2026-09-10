using Shared.Application.Common.Interfaces;

namespace Vendors.Tests.Infrastructure;

/// <summary>
/// Mutable stand-in for JWT-backed CurrentUserService so tests can switch
/// user/role/tenant between workflow steps without HTTP.
/// </summary>
public sealed class FakeCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; }
    public string? Role { get; set; }
    public Guid TenantId { get; set; }

    public void As(string userId, string role, Guid? tenantId = null)
    {
        UserId = userId;
        Role = role;
        if (tenantId.HasValue)
            TenantId = tenantId.Value;
    }
}
