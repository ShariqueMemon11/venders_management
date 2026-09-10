using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return null;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Email == normalizedEmail && u.IsActive && !u.IsDeleted,
                cancellationToken);

        // Always verify — use a real dummy hash when the user is missing so both failure
        // paths pay the same BCrypt cost (no email-enumeration via response timing).
        var hashToVerify = user?.PasswordHash ?? AuthTimingConstants.DummyPasswordHash;
        if (!_passwordHasher.Verify(password, hashToVerify))
            return null;

        if (user is null)
            return null;

        return new AuthenticatedUser(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.TenantId);
    }

    public async Task<bool> IsActiveSessionAsync(
        string? userIdClaim,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userIdClaim))
            return false;

        var normalizedEmail = userIdClaim.Trim().ToLowerInvariant();

        var flags = await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Email == normalizedEmail)
            .Select(u => new { u.IsActive, u.IsDeleted })
            .FirstOrDefaultAsync(cancellationToken);

        return flags is not null && flags.IsActive && !flags.IsDeleted;
    }
}
