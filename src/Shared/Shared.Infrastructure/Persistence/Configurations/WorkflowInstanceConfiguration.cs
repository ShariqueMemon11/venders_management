using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Workflow;
using Platform.Workflow.Entities;
using Platform.Workflow.ValueObjects;

namespace Shared.Infrastructure.Persistence.Configurations;

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("WorkflowInstances");

        builder.HasKey(w => w.Id);

        builder.HasQueryFilter(w => !w.IsDeleted);

        builder.OwnsOne(w => w.TargetEntity, te =>
        {
            te.Property(p => p.Type).HasColumnName("TargetEntityType").IsRequired();
            te.Property(p => p.Id).HasColumnName("TargetEntityId").IsRequired();
        });

        builder.Metadata.FindNavigation(nameof(WorkflowInstance.Tasks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(WorkflowInstance.History))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(WorkflowInstance.Attachments))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(w => w.Tasks)
            .WithOne()
            .HasForeignKey(t => t.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.History)
            .WithOne()
            .HasForeignKey(h => h.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Attachments)
            .WithOne()
            .HasForeignKey(a => a.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

