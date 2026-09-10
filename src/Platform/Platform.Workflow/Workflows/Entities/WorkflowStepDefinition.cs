using Shared.Domain.Common;
using Platform.Workflow.ValueObjects;

namespace Platform.Workflow.Entities;

public class WorkflowStepDefinition : EntityBase
{
    public Guid WorkflowDefinitionId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Order { get; private set; }
    public bool IsFinalStep { get; private set; }
    public Assignment DefaultAssignment { get; private set; }
    public SlaDefinition Sla { get; private set; }

    private WorkflowStepDefinition() { } // For EF Core

    internal WorkflowStepDefinition(
        string name, 
        string description, 
        int order, 
        bool isFinalStep, 
        Assignment defaultAssignment,
        SlaDefinition? sla = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        Order = order;
        IsFinalStep = isFinalStep;
        DefaultAssignment = defaultAssignment ?? throw new ArgumentNullException(nameof(defaultAssignment));
        Sla = sla ?? SlaDefinition.None();
    }
}



