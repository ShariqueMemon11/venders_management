using Microsoft.EntityFrameworkCore;
using Platform.Workflow.Entities;

namespace Platform.Workflow.Interfaces;

public interface IWorkflowDbContext
{
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<WorkflowStepDefinition> WorkflowStepDefinitions { get; }
    DbSet<WorkflowTransition> WorkflowTransitions { get; }
    DbSet<WorkflowCondition> WorkflowConditions { get; }
    DbSet<WorkflowInstance> WorkflowInstances { get; }
    DbSet<WorkflowTask> WorkflowTasks { get; }
    DbSet<WorkflowHistory> WorkflowHistories { get; }
    DbSet<WorkflowAttachment> WorkflowAttachments { get; }

    DbSet<BusinessHoliday> BusinessHolidays { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}