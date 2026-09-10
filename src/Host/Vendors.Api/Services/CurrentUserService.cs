using System.Security.Claims;
using Shared.Application.Common.Interfaces;

namespace Vendors.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    public const string TenantIdClaimType = "TenantId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Role
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var role = user.FindFirstValue(ClaimTypes.Role);
                if (string.IsNullOrWhiteSpace(role))
                {
                    throw new UnauthorizedAccessException(
                        "Authenticated request is missing a valid Role claim.");
                }

                return role;
            }

            // No HTTP user (startup seed / design-time) — not an Admin default.
            return null;
        }
    }

    public Guid TenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var raw = user.FindFirstValue(TenantIdClaimType);
                if (!Guid.TryParse(raw, out var tenantId) || tenantId == Guid.Empty)
                {
                    throw new UnauthorizedAccessException(
                        "Authenticated request is missing a valid TenantId claim.");
                }

                return tenantId;
            }

            // No HTTP user (startup seed / design-time). Callers must set TenantId on entities explicitly.
            return Guid.Empty;
        }
    }
}
