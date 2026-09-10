using Platform.Workflow.ValueObjects;

namespace Platform.Workflow.Repositories;

public interface IWorkflowInstanceRepository
{
    Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowInstance?> GetActiveInstanceForEntityAsync(EntityReference entityReference, CancellationToken cancellationToken = default);
    
    // Using a simple collection for task queries. In CQRS, this might be handled via Dapper reads.
    Task<IReadOnlyList<WorkflowInstance>> GetPendingInstancesForUserAsync(string userId, IEnumerable<string> userRoles, CancellationToken cancellationToken = default);
    
    Task AddAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default);
    void Update(WorkflowInstance workflowInstance);
}


