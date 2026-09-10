using Shared.Domain.Common;

namespace Vendors.Domain.Entities;

public class Tenant : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty; // e.g., "acme-corp"
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Vendor> Vendors { get; set; } = new List<Vendor>();
}


