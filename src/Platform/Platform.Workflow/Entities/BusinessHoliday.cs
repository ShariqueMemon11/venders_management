using Shared.Domain.Common;

namespace Platform.Workflow.Entities;

public class BusinessHoliday : EntityBase
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? TenantId { get; set; } // Null implies it applies globally to all tenants
}