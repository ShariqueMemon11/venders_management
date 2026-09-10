using Shared.Domain.Common;
using Platform.Workflow.Entities;
using Platform.Workflow.Enums;
using Platform.Workflow.Events;
using Platform.Workflow.ValueObjects;

namespace Platform.Workflow;

public class WorkflowInstance : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid WorkflowDefinitionId { get; private set; }
    public EntityReference TargetEntity { get; private set; }
    public WorkflowState CurrentState { get; private set; }
    public Guid? CurrentStepId { get; private set; }

    private readonly List<WorkflowTask> _tasks = new();
    public IReadOnlyCollection<WorkflowTask> Tasks => _tasks.AsReadOnly();

    private readonly List<WorkflowHistory> _history = new();
    public IReadOnlyCollection<WorkflowHistory> History => _history.AsReadOnly();

    private readonly List<WorkflowAttachment> _attachments = new();
    public IReadOnlyCollection<WorkflowAttachment> Attachments => _attachments.AsReadOnly();

    private WorkflowInstance() { } // For EF Core

    private WorkflowInstance(Guid tenantId, Guid workflowDefinitionId, EntityReference targetEntity)
    {
        TenantId = tenantId;
        WorkflowDefinitionId = workflowDefinitionId;
        TargetEntity = targetEntity ?? throw new ArgumentNullException(nameof(targetEntity));
        CurrentState = WorkflowState.Draft;
    }

    public static WorkflowInstance Start(
        Guid tenantId, 
        WorkflowDefinition definition, 
        EntityReference targetEntity, 
        string requesterId,
        Assignment initialAssignment,
        DateTime? initialTaskDueDate = null)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (!definition.IsActive) throw new InvalidOperationException("Cannot start an inactive workflow definition.");

        var instance = new WorkflowInstance(tenantId, definition.Id, targetEntity);
        instance.CurrentState = WorkflowState.InProgress;
        
        var firstStep = definition.Steps.OrderBy(s => s.Order).FirstOrDefault();
        if (firstStep == null) throw new InvalidOperationException("Workflow definition has no steps.");

        instance.CurrentStepId = firstStep.Id;
        
        // Start first task with a cloned assignment to avoid EF owned-type tracking conflicts.
        var taskAssignment = new Assignment(initialAssignment.Strategy, initialAssignment.Value);
        var task = new WorkflowTask(firstStep.Id, taskAssignment, initialTaskDueDate);
        instance._tasks.Add(task);

        instance._history.Add(new WorkflowHistory(WorkflowAction.Approve, requesterId, WorkflowComment.Create("Workflow Started")));

        instance.AddDomainEvent(new WorkflowStartedEvent(instance.Id, targetEntity.Id) { TenantId = tenantId });
        instance.AddDomainEvent(new WorkflowTaskAssignedEvent(task.Id, instance.Id, task.Assignment) { TenantId = tenantId });

        return instance;
    }

    public void ApproveTask(Guid taskId, string actor, string? commentText, WorkflowDefinition definition, Assignment? nextAssignment = null, DateTime? nextDueDate = null)
    {
        var task = GetActiveTask(taskId);
        
        task.Complete(WorkflowState.Approved);
        _history.Add(new WorkflowHistory(WorkflowAction.Approve, actor, WorkflowComment.Create(commentText), taskId));

        var currentStep = definition.Steps.First(s => s.Id == task.StepDefinitionId);

        if (currentStep.IsFinalStep)
        {
            CurrentState = WorkflowState.Approved;
            CurrentStepId = null;
            AddDomainEvent(new WorkflowCompletedEvent(Id, TargetEntity.Id) { TenantId = TenantId });
        }
        else
        {
            // Find next step based on transitions. Simple implementation for now (first matching).
            var transition = definition.Transitions.FirstOrDefault(t => t.FromStepId == currentStep.Id);
            if (transition == null) throw new InvalidOperationException("No valid transition found from the current step.");

            var nextStep = definition.Steps.First(s => s.Id == transition.ToStepId);
            CurrentStepId = nextStep.Id;

            // Resolve next assignment if not provided, always clone to avoid EF owned-type tracking conflicts.
            var sourceAssignment = nextAssignment ?? nextStep.DefaultAssignment;
            var taskAssignment = new Assignment(sourceAssignment.Strategy, sourceAssignment.Value);
            var nextTask = new WorkflowTask(nextStep.Id, taskAssignment, nextDueDate);
            _tasks.Add(nextTask);

            AddDomainEvent(new WorkflowTaskAssignedEvent(nextTask.Id, Id, nextTask.Assignment) { TenantId = TenantId });
        }
    }

    public void RejectTask(Guid taskId, string actor, string commentText)
    {
        if (string.IsNullOrWhiteSpace(commentText))
            throw new ArgumentException("A comment is required when rejecting a task.");

        var task = GetActiveTask(taskId);
        
        task.Complete(WorkflowState.Rejected);
        _history.Add(new WorkflowHistory(WorkflowAction.Reject, actor, WorkflowComment.Create(commentText), taskId));

        CurrentState = WorkflowState.Rejected;
        CurrentStepId = null;

        AddDomainEvent(new WorkflowRejectedEvent(Id, TargetEntity.Id) { TenantId = TenantId });
    }

    public void Cancel(string actor, string commentText)
    {
        if (CurrentState != WorkflowState.InProgress && CurrentState != WorkflowState.Draft)
            throw new InvalidOperationException("Only Draft or InProgress workflows can be cancelled.");

        foreach (var task in _tasks.Where(t => t.Status == WorkflowState.InProgress))
        {
            task.Cancel();
        }

        CurrentState = WorkflowState.Cancelled;
        _history.Add(new WorkflowHistory(WorkflowAction.Cancel, actor, WorkflowComment.Create(commentText)));

        AddDomainEvent(new WorkflowCancelledEvent(Id, TargetEntity.Id, actor));
    }

    public void AddAttachment(string fileName, string filePath, string uploadedBy)
    {
        _attachments.Add(new WorkflowAttachment(fileName, filePath, uploadedBy));
    }

    private WorkflowTask GetActiveTask(Guid taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null) throw new ArgumentException("Task not found.");
        if (task.Status != WorkflowState.InProgress) throw new InvalidOperationException("Task is not active.");
        return task;
    }
}



