using Shared.Domain.Common;

namespace Platform.Workflow.Events;

public class WorkflowCancelledEvent : BaseEvent
{
    public Guid WorkflowInstanceId { get; }
    public Guid EntityId { get; }
    public string CancelledBy { get; }

    public WorkflowCancelledEvent(Guid workflowInstanceId, Guid entityId, string cancelledBy)
    {
        WorkflowInstanceId = workflowInstanceId;
        EntityId = entityId;
        CancelledBy = cancelledBy;
    }
}



