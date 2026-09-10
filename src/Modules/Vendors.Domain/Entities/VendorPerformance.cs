using Shared.Domain.Common;

namespace Vendors.Domain.Entities;

public class VendorPerformance : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VendorId { get; set; }
    
    public string EvaluationPeriod { get; set; } = string.Empty; // e.g. "Q1 2026"
    public decimal QualityScore { get; set; } // Max 100 or 5.0
    public decimal DeliveryScore { get; set; }
    public decimal ResponsivenessScore { get; set; }
    public decimal ComplianceScore { get; set; }
    public decimal AverageScore { get; set; }
    
    public string? Comments { get; set; }
    public string? Evaluator { get; set; }

    // Navigation
    public virtual Vendor Vendor { get; set; } = null!;
}


