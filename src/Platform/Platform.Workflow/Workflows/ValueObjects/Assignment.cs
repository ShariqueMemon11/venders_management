using Platform.Workflow.Enums;

namespace Platform.Workflow.ValueObjects;

public record Assignment(AssignmentStrategy Strategy, string Value)
{
    public static Assignment ToUser(string userId) => new(AssignmentStrategy.User, userId);
    public static Assignment ToRole(string roleName) => new(AssignmentStrategy.Role, roleName);
}


