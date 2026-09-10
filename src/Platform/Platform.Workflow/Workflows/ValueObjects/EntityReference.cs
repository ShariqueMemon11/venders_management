using Platform.Workflow.Enums;

namespace Platform.Workflow.ValueObjects;

public record EntityReference(WorkflowEntityType Type, Guid Id);


