namespace Shared.Domain.Audit;

public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty; // "Added", "Modified", "Deleted"
    public string TableName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string OldValues { get; set; } = "{}";
    public string NewValues { get; set; } = "{}";
    public string AffectedColumns { get; set; } = "[]";
    public string PrimaryKey { get; set; } = "{}";
}