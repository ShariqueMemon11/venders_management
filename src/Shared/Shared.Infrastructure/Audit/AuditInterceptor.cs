using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Audit;
using System.Text.Json;

namespace Shared.Infrastructure.Audit;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        BeforeSaveChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        BeforeSaveChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void BeforeSaveChanges(DbContext? context)
    {
        if (context == null) return;

        context.ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();
        var userId = _currentUserService.UserId ?? "System";

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditEntry || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Metadata.Name,
                UserId = userId,
                ActionType = entry.State.ToString(),
                Timestamp = DateTime.UtcNow
            };

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var affectedColumns = new List<string>();
            var primaryKey = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    primaryKey[propertyName] = property.CurrentValue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[propertyName] = property.CurrentValue;
                        affectedColumns.Add(propertyName);
                        break;
                    case EntityState.Deleted:
                        oldValues[propertyName] = property.OriginalValue;
                        break;
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            oldValues[propertyName] = property.OriginalValue;
                            newValues[propertyName] = property.CurrentValue;
                            affectedColumns.Add(propertyName);
                        }
                        break;
                }
            }

            auditEntry.PrimaryKey = JsonSerializer.Serialize(primaryKey);
            auditEntry.OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : "{}";
            auditEntry.NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : "{}";
            auditEntry.AffectedColumns = affectedColumns.Count > 0 ? JsonSerializer.Serialize(affectedColumns) : "[]";

            auditEntries.Add(auditEntry);
        }

        if (auditEntries.Any())
        {
            context.Set<AuditEntry>().AddRange(auditEntries);
        }
    }
}