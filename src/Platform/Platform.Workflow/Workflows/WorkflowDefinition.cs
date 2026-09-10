using Shared.Domain.Common;
using Platform.Workflow.Entities;
using Platform.Workflow.Enums;
using Platform.Workflow.ValueObjects;

namespace Platform.Workflow;

public class WorkflowDefinition : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public WorkflowEntityType EntityType { get; private set; }
    public int Version { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<WorkflowStepDefinition> _steps = new();
    public IReadOnlyCollection<WorkflowStepDefinition> Steps => _steps.AsReadOnly();

    private readonly List<WorkflowTransition> _transitions = new();
    public IReadOnlyCollection<WorkflowTransition> Transitions => _transitions.AsReadOnly();

    private WorkflowDefinition() { } // For EF Core

    public WorkflowDefinition(Guid tenantId, string name, string description, WorkflowEntityType entityType)
    {
        TenantId = tenantId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        EntityType = entityType;
        Version = 1;
        IsActive = false;
    }

    public WorkflowStepDefinition AddStep(string name, string description, int order, bool isFinal, Assignment assignment, SlaDefinition? sla = null)
    {
        if (IsActive) throw new InvalidOperationException("Cannot modify an active workflow definition. Create a new version.");
        
        var step = new WorkflowStepDefinition(name, description, order, isFinal, assignment, sla);
        _steps.Add(step);
        return step;
    }

    public WorkflowTransition AddTransition(Guid fromStepId, Guid toStepId)
    {
        if (IsActive) throw new InvalidOperationException("Cannot modify an active workflow definition. Create a new version.");
        
        if (!_steps.Any(s => s.Id == fromStepId) || !_steps.Any(s => s.Id == toStepId))
            throw new ArgumentException("Both From and To steps must belong to this workflow definition.");

        var transition = new WorkflowTransition(fromStepId, toStepId);
        _transitions.Add(transition);
        return transition;
    }

    public void Publish()
    {
        if (!_steps.Any()) throw new InvalidOperationException("Cannot publish a workflow with no steps.");
        IsActive = true;
    }

    public WorkflowDefinition CreateNewVersion()
    {
        var newVersion = new WorkflowDefinition(TenantId, Name, Description, EntityType)
        {
            Version = this.Version + 1
        };

        // Deep copy steps
        var stepMap = new Dictionary<Guid, Guid>();
        foreach (var step in _steps)
        {
            var newStep = newVersion.AddStep(step.Name, step.Description, step.Order, step.IsFinalStep, step.DefaultAssignment, step.Sla);
            stepMap[step.Id] = newStep.Id;
        }

        // Deep copy transitions
        foreach (var transition in _transitions)
        {
            var newTransition = newVersion.AddTransition(stepMap[transition.FromStepId], stepMap[transition.ToStepId]);
            foreach (var cond in transition.Conditions)
            {
                newTransition.AddCondition(cond.FieldName, cond.Operator, cond.Value);
            }
        }

        this.IsActive = false; // Deactivate current version

        return newVersion;
    }
}



