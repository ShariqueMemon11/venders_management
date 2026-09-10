using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Dtos;

public class VendorDirectoryDto
{
    public Guid Id { get; set; }
    public string VendorNumber { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string? TaxRegistrationNumber { get; set; }
    public VendorStatus Status { get; set; }
    public string? StatusName => Status.ToString();
    public DateTime CreatedAt { get; set; }
    
    // Contact Info (Primary)
    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactPhone { get; set; }
    
    // Location Info (Primary/HQ)
    public string? PrimaryAddressCity { get; set; }
    public string? PrimaryAddressCountry { get; set; }
}
