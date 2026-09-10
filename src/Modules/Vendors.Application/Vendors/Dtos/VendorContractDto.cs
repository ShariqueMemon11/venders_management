using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Dtos;

public class VendorContractDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddYears(1);
    public decimal ContractValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? PaymentTerms { get; set; }
    public string? SlaBrief { get; set; }
    public ContractStatus Status { get; set; }
    public string StatusName => Status.ToString();
}

