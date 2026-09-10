using Shared.Domain.Common;
using Platform.Workflow.Enums;
using Platform.Workflow.ValueObjects;

namespace Platform.Workflow.Entities;

public class WorkflowTask : EntityBase
{
    public Guid WorkflowInstanceId { get; private set; }
    public Guid StepDefinitionId { get; private set; }
    public Assignment Assignment { get; private set; }
    public WorkflowState Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? DueDate { get; private set; }

    private WorkflowTask() { } // For EF Core

    internal WorkflowTask(Guid stepDefinitionId, Assignment assignment, DateTime? dueDate = null)
    {
        StepDefinitionId = stepDefinitionId;
        Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
        Status = WorkflowState.InProgress;
        CreatedAt = DateTime.UtcNow;
        DueDate = dueDate;
    }

    internal void Complete(WorkflowState finalState)
    {
        if (Status != WorkflowState.InProgress)
            throw new InvalidOperationException("Task is already completed or cancelled.");
            
        Status = finalState;
        CompletedAt = DateTime.UtcNow;
    }

    internal void Cancel()
    {
        if (Status == WorkflowState.InProgress)
        {
            Status = WorkflowState.Cancelled;
            CompletedAt = DateTime.UtcNow;
        }
    }
}



