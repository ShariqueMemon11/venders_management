using Platform.Workflow.Enums;
using Platform.Workflow.Services;
using Platform.Workflow.ValueObjects;

namespace Platform.Workflow.Services;

public class WorkflowAssignmentResolver : IWorkflowAssignmentResolver
{
    public Task<Assignment> ResolveAssignmentAsync(
        Assignment strategyAssignment, 
        Guid targetEntityId, 
        string requesterId, 
        CancellationToken cancellationToken = default)
    {
        // In a real-world scenario, ManagerOfRequester would look up AD/Database, 
        // DepartmentQueue might look up departmental groups, etc.
        switch (strategyAssignment.Strategy)
        {
            case AssignmentStrategy.User:
            case AssignmentStrategy.Role:
                // Return a copy so EF does not track step-owned Assignment on task entities.
                return Task.FromResult(new Assignment(strategyAssignment.Strategy, strategyAssignment.Value));

            case AssignmentStrategy.ManagerOfRequester:
                // Mock implementation: assign to a specific manager user
                return Task.FromResult(Assignment.ToUser("manager@example.com"));

            case AssignmentStrategy.DepartmentQueue:
                // Mock implementation: assign to procurement role
                return Task.FromResult(Assignment.ToRole("ProcurementManager"));

            case AssignmentStrategy.DynamicResolver:
                // Might call an external rules engine, etc.
                return Task.FromResult(Assignment.ToRole("Admin"));

            default:
                throw new NotImplementedException($"Assignment strategy {strategyAssignment.Strategy} is not implemented.");
        }
    }
}


