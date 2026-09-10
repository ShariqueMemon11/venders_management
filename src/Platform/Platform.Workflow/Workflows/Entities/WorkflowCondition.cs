using Shared.Domain.Common;

namespace Platform.Workflow.Entities;

public class WorkflowCondition : EntityBase
{
    public Guid WorkflowTransitionId { get; private set; }
    public string FieldName { get; private set; }
    public string Operator { get; private set; } // e.g. Equals, GreaterThan, Contains
    public string Value { get; private set; }

    private WorkflowCondition() { } // For EF Core

    internal WorkflowCondition(string fieldName, string @operator, string value)
    {
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        Operator = @operator ?? throw new ArgumentNullException(nameof(@operator));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}



