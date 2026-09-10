using Shared.Domain.Common;
using Platform.Workflow.ValueObjects;

namespace Platform.Workflow.Events;

public class WorkflowTaskAssignedEvent : BaseEvent
{
    public Guid TaskId { get; }
    public Guid WorkflowInstanceId { get; }
    public Assignment Assignment { get; }

    /// <summary>
    /// Carried for background outbox processing (no HTTP tenant context).
    /// Optional on older serialized messages — handlers fall back to a filter-bypassing lookup.
    /// </summary>
    public Guid TenantId { get; init; }

    public WorkflowTaskAssignedEvent(Guid taskId, Guid workflowInstanceId, Assignment assignment)
    {
        TaskId = taskId;
        WorkflowInstanceId = workflowInstanceId;
        Assignment = assignment;
    }
}



