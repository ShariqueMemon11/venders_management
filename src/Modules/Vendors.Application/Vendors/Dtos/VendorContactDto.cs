using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Dtos;

public class VendorContactDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public ContactType ContactType { get; set; }
    public string ContactTypeName => ContactType.ToString();
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

