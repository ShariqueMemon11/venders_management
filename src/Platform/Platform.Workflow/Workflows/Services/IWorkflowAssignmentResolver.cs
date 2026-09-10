using Platform.Workflow.ValueObjects;

namespace Platform.Workflow.Services;

public interface IWorkflowAssignmentResolver
{
    /// <summary>
    /// Resolves an assignment definition into specific user IDs or Role names.
    /// Used by the application layer to translate abstract assignments (like "ManagerOfRequester")
    /// into concrete actionable assignments when moving steps.
    /// </summary>
    Task<Assignment> ResolveAssignmentAsync(
        Assignment strategyAssignment, 
        Guid targetEntityId, 
        string requesterId, 
        CancellationToken cancellationToken = default);
}


