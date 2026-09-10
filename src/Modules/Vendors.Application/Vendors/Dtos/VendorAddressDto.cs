using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Dtos;

public class VendorAddressDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public AddressType AddressType { get; set; }
    public string AddressTypeName => AddressType.ToString();
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
}

