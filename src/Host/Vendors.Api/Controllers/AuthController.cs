using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Application.Authorization;
using Shared.Application.Common.Interfaces;
using Vendors.Api.Services;

namespace Vendors.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        ICurrentUserService currentUserService,
        IAuthService authService,
        IJwtTokenService jwtTokenService)
    {
        _currentUserService = currentUserService;
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _authService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (user is null)
            return Unauthorized(new { message = "Invalid email or password" });

        var permissions = AppPermissions.ForRole(user.Role).ToArray();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role),
            new(CurrentUserService.TenantIdClaimType, user.TenantId.ToString())
        };

        foreach (var permission in permissions)
            claims.Add(new Claim(AppPermissions.ClaimType, permission));

        var token = _jwtTokenService.CreateToken(claims, DateTime.UtcNow.AddHours(24));

        return Ok(new
        {
            token,
            user = new
            {
                email = user.Email,
                name = user.DisplayName,
                role = user.Role,
                permissions
            }
        });
    }

    /// <summary>Identity as resolved by CurrentUserService (verifies TenantId comes from the JWT claim).</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = _currentUserService.UserId,
            role = _currentUserService.Role,
            tenantId = _currentUserService.TenantId,
            claimTenantId = User.FindFirstValue(CurrentUserService.TenantIdClaimType)
        });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
