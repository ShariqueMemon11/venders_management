namespace Shared.Application.Common.Interfaces;

/// <summary>
/// Authenticated user resolved from the Users store (post password verification).
/// </summary>
public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    Guid TenantId);
