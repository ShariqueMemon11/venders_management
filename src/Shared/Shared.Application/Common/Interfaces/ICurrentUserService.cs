namespace Shared.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Role { get; }
    Guid TenantId { get; }
}

