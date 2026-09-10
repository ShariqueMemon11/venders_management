using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Workflow.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable("WorkflowTransitions");

        builder.HasKey(t => t.Id);

        builder.Metadata.FindNavigation(nameof(WorkflowTransition.Conditions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Conditions)
            .WithOne()
            .HasForeignKey(c => c.WorkflowTransitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkflowConditionConfiguration : IEntityTypeConfiguration<WorkflowCondition>
{
    public void Configure(EntityTypeBuilder<WorkflowCondition> builder)
    {
        builder.ToTable("WorkflowConditions");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FieldName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Operator).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Value).IsRequired().HasMaxLength(500);
    }
}

