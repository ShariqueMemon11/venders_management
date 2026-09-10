using Shared.Application.Common.Models;
using Shared.Application.Identity;

namespace Shared.Application.Common.Interfaces;

public interface IUserManagementService
{
    Task<Result<Guid>> CreateAsync(CreateUserRequest request, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<UserListItemDto>>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result<bool>> SetActiveAsync(Guid userId, bool isActive, Guid tenantId, string? actorUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> TerminateAsync(Guid userId, Guid tenantId, string? actorUserId, CancellationToken cancellationToken = default);
}
