using Shared.Domain.Common;

namespace Platform.Workflow.Events;

public class WorkflowStartedEvent : BaseEvent
{
    public Guid WorkflowInstanceId { get; }
    public Guid EntityId { get; }

    /// <summary>
    /// Carried for background outbox processing (no HTTP tenant context).
    /// Optional on older serialized messages — handlers fall back to a filter-bypassing lookup.
    /// </summary>
    public Guid TenantId { get; init; }

    public WorkflowStartedEvent(Guid workflowInstanceId, Guid entityId)
    {
        WorkflowInstanceId = workflowInstanceId;
        EntityId = entityId;
    }
}



