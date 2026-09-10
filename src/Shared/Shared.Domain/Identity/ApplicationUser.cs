namespace Shared.Domain.Identity;

/// <summary>
/// Tenant-scoped application user. Not ITenantEntity — login queries run without tenant context.
/// </summary>
public class ApplicationUser : Common.AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public bool IsActive { get; set; } = true;
}
