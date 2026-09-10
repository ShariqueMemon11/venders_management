using Microsoft.EntityFrameworkCore;
using Platform.Workflow;
using Platform.Workflow.Enums;
using Platform.Workflow.Repositories;

namespace Shared.Infrastructure.Persistence.Repositories;

public class WorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly ApplicationDbContext _context;

    public WorkflowDefinitionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .AsNoTracking()
            .Include(w => w.Steps)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.Conditions)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<WorkflowDefinition?> GetActiveDefinitionAsync(WorkflowEntityType entityType, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .Include(w => w.Steps)
            .Include(w => w.Transitions)
                .ThenInclude(t => t.Conditions)
            .Where(w => w.EntityType == entityType && w.IsActive)
            .OrderByDescending(w => w.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> GetAllVersionsAsync(WorkflowEntityType entityType, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .Where(w => w.EntityType == entityType)
            .OrderByDescending(w => w.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
    {
        await _context.WorkflowDefinitions.AddAsync(workflowDefinition, cancellationToken);
    }

    public void Update(WorkflowDefinition workflowDefinition)
    {
        _context.WorkflowDefinitions.Update(workflowDefinition);
    }
}

