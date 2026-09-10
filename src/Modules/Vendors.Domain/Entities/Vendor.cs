using Shared.Domain.Common;
using Vendors.Domain.Enums;

namespace Vendors.Domain.Entities;

public class Vendor : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string VendorNumber { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public VendorStatus Status { get; set; } = VendorStatus.Draft;
    
    public string? TaxRegistrationNumber { get; set; }
    public string? Website { get; set; }
    
    // Financials
    public string? CurrencyCode { get; set; } // ISO 4217
    
    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    
    public virtual ICollection<VendorContact> Contacts { get; set; } = new List<VendorContact>();
    public virtual ICollection<VendorAddress> Addresses { get; set; } = new List<VendorAddress>();
    public virtual ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
    public virtual ICollection<VendorDocument> Documents { get; set; } = new List<VendorDocument>();
    public virtual ICollection<VendorContract> Contracts { get; set; } = new List<VendorContract>();
    public virtual ICollection<VendorCompliance> Compliances { get; set; } = new List<VendorCompliance>();
    public virtual ICollection<VendorPerformance> Performances { get; set; } = new List<VendorPerformance>();
    public virtual ICollection<VendorRisk> Risks { get; set; } = new List<VendorRisk>();
}



