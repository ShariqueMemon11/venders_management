using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Workflow.Entities;
using Platform.Workflow.ValueObjects;

namespace Shared.Infrastructure.Persistence.Configurations;

public class WorkflowTaskConfiguration : IEntityTypeConfiguration<WorkflowTask>
{
    public void Configure(EntityTypeBuilder<WorkflowTask> builder)
    {
        builder.ToTable("WorkflowTasks");

        builder.HasKey(t => t.Id);

        builder.OwnsOne(t => t.Assignment, a =>
        {
            a.Property(p => p.Strategy).HasColumnName("AssignmentStrategy").IsRequired();
            a.Property(p => p.Value).HasColumnName("AssignmentValue").IsRequired().HasMaxLength(200);
        });
    }
}

public class WorkflowHistoryConfiguration : IEntityTypeConfiguration<WorkflowHistory>
{
    public void Configure(EntityTypeBuilder<WorkflowHistory> builder)
    {
        builder.ToTable("WorkflowHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Actor).IsRequired().HasMaxLength(200);

        builder.Property(h => h.Comment)
            .HasConversion(
                c => c == null ? null : c.Value,
                v => v == null ? null : WorkflowComment.Create(v))
            .HasMaxLength(2000);
    }
}

public class WorkflowAttachmentConfiguration : IEntityTypeConfiguration<WorkflowAttachment>
{
    public void Configure(EntityTypeBuilder<WorkflowAttachment> builder)
    {
        builder.ToTable("WorkflowAttachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.FilePath).IsRequired().HasMaxLength(1000);
        builder.Property(a => a.UploadedBy).IsRequired().HasMaxLength(200);
    }
}

