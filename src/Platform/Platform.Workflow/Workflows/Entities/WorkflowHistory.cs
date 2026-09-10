using Shared.Domain.Common;
using Platform.Workflow.Enums;
using Platform.Workflow.ValueObjects;

namespace Platform.Workflow.Entities;

public class WorkflowHistory : EntityBase
{
    public Guid WorkflowInstanceId { get; private set; }
    public Guid? TaskId { get; private set; }
    public WorkflowAction Action { get; private set; }
    public string Actor { get; private set; }
    public WorkflowComment? Comment { get; private set; }
    public DateTime Timestamp { get; private set; }

    private WorkflowHistory() { } // For EF Core

    internal WorkflowHistory(WorkflowAction action, string actor, WorkflowComment? comment, Guid? taskId = null)
    {
        Action = action;
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        Comment = comment;
        TaskId = taskId;
        Timestamp = DateTime.UtcNow;
    }
}



