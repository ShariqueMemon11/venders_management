using Shared.Domain.Common;
using Vendors.Domain.Enums;

namespace Vendors.Domain.Entities;

public class VendorContact : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VendorId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public ContactType ContactType { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Vendor Vendor { get; set; } = null!;
}


