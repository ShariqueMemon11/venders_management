using Shared.Domain.Common;
using Vendors.Domain.Enums;

namespace Vendors.Domain.Entities;

public class VendorContract : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VendorId { get; set; }
    
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ContractValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? PaymentTerms { get; set; }
    public string? SlaBrief { get; set; }
    
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    // Navigation
    public virtual Vendor Vendor { get; set; } = null!;
}


