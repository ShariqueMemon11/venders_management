namespace Vendors.Application.Vendors.Dtos;

public class BankAccountDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? Iban { get; set; }
    public string? SwiftBic { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsVerified { get; set; }
    public bool IsApproved { get; set; }
}

