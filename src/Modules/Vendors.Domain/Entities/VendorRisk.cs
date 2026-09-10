using Shared.Domain.Common;
using Vendors.Domain.Enums;

namespace Vendors.Domain.Entities;

public class VendorRisk : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VendorId { get; set; }
    
    public string RiskCategory { get; set; } = string.Empty; // e.g. "Financial", "Operational"
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public string? RiskDescription { get; set; }
    public string? MitigationPlan { get; set; }
    public DateTime? LastReviewDate { get; set; }

    // Navigation
    public virtual Vendor Vendor { get; set; } = null!;
}


