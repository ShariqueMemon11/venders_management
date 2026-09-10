using Platform.Workflow.Enums;

namespace Platform.Workflow.Interfaces;

public interface IWorkflowEngineService
{
    /// <summary>
    /// Starts a workflow for a given entity type and id.
    /// </summary>
    Task<Guid> StartWorkflowAsync(WorkflowEntityType entityType, Guid entityId, string comments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a specific task within a workflow.
    /// </summary>
    Task ApproveTaskAsync(Guid workflowInstanceId, Guid taskId, string comments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a specific task, stopping the workflow.
    /// </summary>
    Task RejectTaskAsync(Guid workflowInstanceId, Guid taskId, string comments, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels a workflow in progress.
    /// </summary>
    Task CancelWorkflowAsync(Guid workflowInstanceId, string comments, CancellationToken cancellationToken = default);
}


