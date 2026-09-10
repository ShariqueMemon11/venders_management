namespace Shared.Application.Identity;

public class CreateUserRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserListItemDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public static class AssignableUserRoles
{
    public static readonly string[] Values =
    {
        "ProcurementManager",
        "RiskAndCompliance",
        "Viewer"
    };

    public static bool IsAllowed(string? role) =>
        !string.IsNullOrWhiteSpace(role) &&
        Values.Contains(role, StringComparer.Ordinal);
}
