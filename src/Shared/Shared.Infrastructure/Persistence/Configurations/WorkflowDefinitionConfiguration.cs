using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Workflow;
using Platform.Workflow.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Description).HasMaxLength(1000);

        builder.HasQueryFilter(w => !w.IsDeleted);

        builder.Metadata.FindNavigation(nameof(WorkflowDefinition.Steps))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(WorkflowDefinition.Transitions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(w => w.Steps)
            .WithOne()
            .HasForeignKey(s => s.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Transitions)
            .WithOne()
            .HasForeignKey(t => t.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

