using Shared.Domain.Common;

namespace Vendors.Domain.Entities;

public class BankAccount : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VendorId { get; set; }
    
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public string? SwiftBic { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsVerified { get; set; }
    public bool IsApproved { get; set; }

    // Navigation
    public virtual Vendor Vendor { get; set; } = null!;
}


