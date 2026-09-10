using Microsoft.EntityFrameworkCore;
using Platform.Workflow;
using Platform.Workflow.Enums;
using Platform.Workflow.Repositories;
using Platform.Workflow.ValueObjects;

namespace Shared.Infrastructure.Persistence.Repositories;

public class WorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly ApplicationDbContext _context;

    public WorkflowInstanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowInstances
            .Include(w => w.Tasks)
            .Include(w => w.History)
            .Include(w => w.Attachments)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<WorkflowInstance?> GetActiveInstanceForEntityAsync(EntityReference entityReference, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowInstances
            .Include(w => w.Tasks)
            .Include(w => w.History)
            .Include(w => w.Attachments)
            .Where(w => w.TargetEntity.Type == entityReference.Type 
                     && w.TargetEntity.Id == entityReference.Id 
                     && (w.CurrentState == WorkflowState.Draft || w.CurrentState == WorkflowState.InProgress))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowInstance>> GetPendingInstancesForUserAsync(string userId, IEnumerable<string> userRoles, CancellationToken cancellationToken = default)
    {
        // Advanced querying for Tasks assigned to user or their roles
        var rolesList = userRoles.ToList();
        
        return await _context.WorkflowInstances
            .Include(w => w.Tasks)
            .Where(w => w.CurrentState == WorkflowState.InProgress &&
                        w.Tasks.Any(t => t.Status == WorkflowState.InProgress && 
                                         ( (t.Assignment.Strategy == AssignmentStrategy.User && t.Assignment.Value == userId) ||
                                           (t.Assignment.Strategy == AssignmentStrategy.Role && rolesList.Contains(t.Assignment.Value)) )))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
    {
        await _context.WorkflowInstances.AddAsync(workflowInstance, cancellationToken);
    }

    public void Update(WorkflowInstance workflowInstance)
    {
        // Avoid forcing the whole graph to Modified when EF is already tracking changes.
        if (_context.Entry(workflowInstance).State == EntityState.Detached)
        {
            _context.WorkflowInstances.Update(workflowInstance);
        }
    }
}

