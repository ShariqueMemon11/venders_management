using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Platform.Workflow.Interfaces;
using Platform.Workflow;
using Platform.Workflow.Entities;
using Platform.Workflow.Enums;
using Platform.Workflow.Repositories;
using Platform.Workflow.Services;
using Platform.Workflow.ValueObjects;

namespace Platform.Workflow.Services;

public class WorkflowEngineService : IWorkflowEngineService
{
    private readonly IWorkflowDefinitionRepository _definitionRepository;
    private readonly IWorkflowInstanceRepository _instanceRepository;
    private readonly IWorkflowAssignmentResolver _assignmentResolver;
    private readonly ISlaCalculator _slaCalculator;
    private readonly Shared.Application.Common.Interfaces.ICurrentUserService _currentUserService;
    private readonly IWorkflowDbContext _context;

    public WorkflowEngineService(
        IWorkflowDefinitionRepository definitionRepository,
        IWorkflowInstanceRepository instanceRepository,
        IWorkflowAssignmentResolver assignmentResolver,
        ISlaCalculator slaCalculator,
        Shared.Application.Common.Interfaces.ICurrentUserService currentUserService,
        IWorkflowDbContext context)
    {
        _definitionRepository = definitionRepository;
        _instanceRepository = instanceRepository;
        _assignmentResolver = assignmentResolver;
        _slaCalculator = slaCalculator;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<Guid> StartWorkflowAsync(WorkflowEntityType entityType, Guid entityId, string comments, CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserService.TenantId;
        var userId = _currentUserService.UserId ?? "system";

        // Find active definition
        var definition = await _definitionRepository.GetActiveDefinitionAsync(entityType, cancellationToken);
        if (definition == null)
            throw new InvalidOperationException($"No active workflow definition found for entity type {entityType}.");

        var firstStep = definition.Steps.OrderBy(s => s.Order).FirstOrDefault();
        if (firstStep == null)
            throw new InvalidOperationException("Workflow definition has no steps.");

        // Resolve first task assignment and SLA
        var assignment = await _assignmentResolver.ResolveAssignmentAsync(firstStep.DefaultAssignment, entityId, userId, cancellationToken);
        var dueDate = await _slaCalculator.CalculateDueDateAsync(firstStep.Sla, DateTime.UtcNow, cancellationToken);

        // Check if an active instance already exists
        var entityRef = new EntityReference(entityType, entityId);
        var existingInstance = await _instanceRepository.GetActiveInstanceForEntityAsync(entityRef, cancellationToken);
        if (existingInstance != null)
            throw new InvalidOperationException("An active workflow instance already exists for this entity.");

        // Start instance
        var instance = WorkflowInstance.Start(tenantId, definition, entityRef, userId, assignment, dueDate);
        
        if (!string.IsNullOrWhiteSpace(comments))
        {
            // Adding initial comment via reflection/internal or just keeping standard history. 
            // Start automatically adds "Workflow Started" comment. We could append the user comment.
        }

        await _instanceRepository.AddAsync(instance, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return instance.Id;
    }

    public async Task ApproveTaskAsync(Guid workflowInstanceId, Guid taskId, string comments, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? "system";
        
        var instance = await _instanceRepository.GetByIdAsync(workflowInstanceId, cancellationToken);
        if (instance == null) throw new KeyNotFoundException("Workflow instance not found.");

        var definition = await _definitionRepository.GetByIdAsync(instance.WorkflowDefinitionId, cancellationToken);
        if (definition == null) throw new InvalidOperationException("Workflow definition not found.");

        var task = instance.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null) throw new KeyNotFoundException("Task not found.");

        var currentStep = definition.Steps.FirstOrDefault(s => s.Id == task.StepDefinitionId);
        if (currentStep == null) throw new InvalidOperationException("Current step definition not found.");

        Assignment? nextResolvedAssignment = null;
        DateTime? nextDueDate = null;

        if (!currentStep.IsFinalStep)
        {
            // Find next step based on transitions (simple routing)
            var transition = definition.Transitions.FirstOrDefault(t => t.FromStepId == currentStep.Id);
            if (transition != null)
            {
                var nextStep = definition.Steps.FirstOrDefault(s => s.Id == transition.ToStepId);
                if (nextStep != null)
                {
                    nextResolvedAssignment = await _assignmentResolver.ResolveAssignmentAsync(nextStep.DefaultAssignment, instance.TargetEntity.Id, userId, cancellationToken);
                    nextDueDate = await _slaCalculator.CalculateDueDateAsync(nextStep.Sla, DateTime.UtcNow, cancellationToken);
                }
            }
        }

        instance.ApproveTask(taskId, userId, comments, definition, nextResolvedAssignment, nextDueDate);
        
        await PrepareNewChildEntitiesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectTaskAsync(Guid workflowInstanceId, Guid taskId, string comments, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? "system";
        
        var instance = await _instanceRepository.GetByIdAsync(workflowInstanceId, cancellationToken);
        if (instance == null) throw new KeyNotFoundException("Workflow instance not found.");

        instance.RejectTask(taskId, userId, comments);
        
        await PrepareNewChildEntitiesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelWorkflowAsync(Guid workflowInstanceId, string comments, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId ?? "system";
        
        var instance = await _instanceRepository.GetByIdAsync(workflowInstanceId, cancellationToken);
        if (instance == null) throw new KeyNotFoundException("Workflow instance not found.");

        instance.Cancel(userId, comments);
        
        await PrepareNewChildEntitiesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task PrepareNewChildEntitiesAsync(CancellationToken cancellationToken)
    {
        if (_context is not DbContext dbContext)
        {
            return;
        }

        dbContext.ChangeTracker.DetectChanges();
        await EnsureInsertedAsync<WorkflowTask>(dbContext, cancellationToken);
        await EnsureInsertedAsync<WorkflowHistory>(dbContext, cancellationToken);
    }

    private static async Task EnsureInsertedAsync<TEntity>(DbContext dbContext, CancellationToken cancellationToken)
        where TEntity : class
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<TEntity>()
            .Where(e => e.State == EntityState.Modified)
            .ToList())
        {
            if (entry.Property("Id").CurrentValue is not Guid entityId)
            {
                continue;
            }

            var exists = await dbContext.Set<TEntity>()
                .AsNoTracking()
                .AnyAsync(e => EF.Property<Guid>(e, "Id") == entityId, cancellationToken);

            if (!exists)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}



