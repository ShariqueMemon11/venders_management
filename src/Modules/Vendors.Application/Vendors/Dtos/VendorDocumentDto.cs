using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Dtos;

public class VendorDocumentDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public string DocumentTypeName => DocumentType.ToString();
    public string StoragePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DocumentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? VerificationComments { get; set; }
}

