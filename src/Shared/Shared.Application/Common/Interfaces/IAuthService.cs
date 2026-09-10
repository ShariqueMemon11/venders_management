namespace Shared.Application.Common.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Validates email/password against the Users store.
    /// Returns null for unknown email, wrong password, inactive, or deleted users.
    /// </summary>
    Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Per-request check for an already-issued JWT. True only when the user exists,
    /// IsActive, and is not deleted. <paramref name="userIdClaim"/> is the token's
    /// NameIdentifier (email), the same value CurrentUserService.UserId exposes.
    /// </summary>
    Task<bool> IsActiveSessionAsync(string? userIdClaim, CancellationToken cancellationToken = default);
}
