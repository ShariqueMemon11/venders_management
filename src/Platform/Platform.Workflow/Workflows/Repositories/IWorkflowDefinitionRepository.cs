using Platform.Workflow.Enums;

namespace Platform.Workflow.Repositories;

public interface IWorkflowDefinitionRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> GetActiveDefinitionAsync(WorkflowEntityType entityType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDefinition>> GetAllVersionsAsync(WorkflowEntityType entityType, CancellationToken cancellationToken = default);
    
    Task AddAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
    void Update(WorkflowDefinition workflowDefinition);
}


