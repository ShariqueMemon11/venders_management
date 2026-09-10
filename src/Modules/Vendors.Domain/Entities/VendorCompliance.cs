using Shared.Domain.Common;
using Vendors.Domain.Enums;

namespace Vendors.Domain.Entities;

public class VendorCompliance : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VendorId { get; set; }
    
    public string RequirementName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ComplianceStatus Status { get; set; } = ComplianceStatus.UnderReview;
    public DateTime? CheckedAt { get; set; }
    public string? CheckedBy { get; set; }
    public string? Comments { get; set; }

    // Navigation
    public virtual Vendor Vendor { get; set; } = null!;
}


