using Shared.Domain.Common;

namespace Platform.Workflow.Entities;

public class WorkflowTransition : EntityBase
{
    public Guid WorkflowDefinitionId { get; private set; }
    public Guid FromStepId { get; private set; }
    public Guid ToStepId { get; private set; }
    
    private readonly List<WorkflowCondition> _conditions = new();
    public IReadOnlyCollection<WorkflowCondition> Conditions => _conditions.AsReadOnly();

    private WorkflowTransition() { } // For EF Core

    internal WorkflowTransition(Guid fromStepId, Guid toStepId)
    {
        FromStepId = fromStepId;
        ToStepId = toStepId;
    }

    public void AddCondition(string fieldName, string @operator, string value)
    {
        _conditions.Add(new WorkflowCondition(fieldName, @operator, value));
    }
}



