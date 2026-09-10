using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Workflow.Entities;

namespace Shared.Infrastructure.Persistence.Configurations;

public class WorkflowStepDefinitionConfiguration : IEntityTypeConfiguration<WorkflowStepDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowStepDefinition> builder)
    {
        builder.ToTable("WorkflowStepDefinitions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(1000);

        builder.OwnsOne(s => s.DefaultAssignment, a =>
        {
            a.Property(p => p.Strategy).HasColumnName("AssignmentStrategy").IsRequired();
            a.Property(p => p.Value).HasColumnName("AssignmentValue").IsRequired().HasMaxLength(200);
        });

        builder.OwnsOne(s => s.Sla, sla =>
        {
            // SQL Server `time` maxes out at 24 hours; SLA days must be stored as ticks.
            sla.Property(p => p.Duration)
                .HasColumnName("SlaDuration")
                .HasConversion(v => v.Ticks, v => TimeSpan.FromTicks(v))
                .HasColumnType("bigint");
            sla.Property(p => p.IsBusinessDays).HasColumnName("SlaIsBusinessDays");
        });
    }
}

