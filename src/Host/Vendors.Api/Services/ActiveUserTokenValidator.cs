using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Shared.Application.Common.Interfaces;

namespace Vendors.Api.Services;

/// <summary>
/// JWT Bearer <see cref="JwtBearerEvents.OnTokenValidated"/> handler: rejects a
/// cryptographically valid token when the user has been deactivated or terminated.
/// Resolves identity from the same NameIdentifier claim as <see cref="CurrentUserService.UserId"/>.
/// </summary>
public static class ActiveUserTokenValidator
{
    public static async Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var auth = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();

        if (!await auth.IsActiveSessionAsync(userId, context.HttpContext.RequestAborted))
            context.Fail("User is no longer active.");
    }
}
