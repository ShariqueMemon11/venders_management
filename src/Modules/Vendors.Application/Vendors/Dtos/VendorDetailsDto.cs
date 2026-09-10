using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Dtos;

public class VendorDetailsDto
{
    public Guid Id { get; set; }
    public string VendorNumber { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public VendorStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? TaxRegistrationNumber { get; set; }
    public string? Website { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Sub-modules collections
    public List<VendorContactDto> Contacts { get; set; } = new();
    public List<VendorAddressDto> Addresses { get; set; } = new();
    public List<BankAccountDto> BankAccounts { get; set; } = new();
    public List<VendorDocumentDto> Documents { get; set; } = new();
    public List<VendorContractDto> Contracts { get; set; } = new();
    public List<VendorComplianceDto> Compliances { get; set; } = new();
    public List<VendorPerformanceDto> Performances { get; set; } = new();
    public List<VendorRiskDto> Risks { get; set; } = new();
}

