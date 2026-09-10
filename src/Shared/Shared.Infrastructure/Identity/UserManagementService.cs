using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Application.Common.Models;
using Shared.Application.Identity;
using Shared.Domain.Identity;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.Identity;

public sealed class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UserManagementService(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<Result<Guid>> CreateAsync(
        CreateUserRequest request,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            return Result<Guid>.FailureResult("Tenant is required.");

        if (!AssignableUserRoles.IsAllowed(request.Role))
            return Result<Guid>.FailureResult("Role must be ProcurementManager, RiskAndCompliance, or Viewer.");

        var email = NormalizeEmail(request.Email);
        var taken = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email, cancellationToken);
        if (taken)
            return Result<Guid>.FailureResult("Email is already in use.");

        var user = new ApplicationUser
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            Role = request.Role,
            TenantId = tenantId,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.SuccessResult(user.Id);
    }

    public async Task<Result<IReadOnlyList<UserListItemDto>>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.Email)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                IsDeleted = u.IsDeleted,
                Status = u.IsDeleted ? "Terminated" : u.IsActive ? "Active" : "Inactive",
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UserListItemDto>>.SuccessResult(users);
    }

    public async Task<Result<bool>> SetActiveAsync(
        Guid userId,
        bool isActive,
        Guid tenantId,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await FindInTenantAsync(userId, tenantId, cancellationToken);
        if (user is null)
            return Result<bool>.FailureResult("User was not found.");

        if (!isActive && IsSelf(user, actorUserId))
            return Result<bool>.FailureResult("You cannot deactivate your own account.");

        if (user.IsDeleted)
            return Result<bool>.FailureResult("Terminated users cannot be reactivated.");

        user.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.SuccessResult(true);
    }

    public async Task<Result<bool>> TerminateAsync(
        Guid userId,
        Guid tenantId,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await FindInTenantAsync(userId, tenantId, cancellationToken);
        if (user is null)
            return Result<bool>.FailureResult("User was not found.");

        if (IsSelf(user, actorUserId))
            return Result<bool>.FailureResult("You cannot terminate your own account.");

        if (user.IsDeleted)
            return Result<bool>.FailureResult("User is already terminated.");

        user.IsDeleted = true;
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = actorUserId;
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.SuccessResult(true);
    }

    private async Task<ApplicationUser?> FindInTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken) =>
        await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);

    private static bool IsSelf(ApplicationUser user, string? actorUserId) =>
        !string.IsNullOrWhiteSpace(actorUserId) &&
        string.Equals(user.Email, actorUserId.Trim(), StringComparison.OrdinalIgnoreCase);
}
