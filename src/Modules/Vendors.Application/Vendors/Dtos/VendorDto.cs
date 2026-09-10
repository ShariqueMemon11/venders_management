using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Dtos;

public class VendorDto
{
    public Guid Id { get; set; }
    public string VendorNumber { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public VendorStatus Status { get; set; }
    public string? StatusName => Status.ToString();
    public string? TaxRegistrationNumber { get; set; }
    public string? Website { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime CreatedAt { get; set; }
}

