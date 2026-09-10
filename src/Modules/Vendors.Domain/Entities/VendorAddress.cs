using Shared.Domain.Common;
using Vendors.Domain.Enums;

namespace Vendors.Domain.Entities;

public class VendorAddress : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VendorId { get; set; }
    
    public AddressType AddressType { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? PostalCode { get; set; }

    // Navigation
    public virtual Vendor Vendor { get; set; } = null!;
}


