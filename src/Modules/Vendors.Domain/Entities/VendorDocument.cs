using Shared.Domain.Common;
using Vendors.Domain.Enums;

namespace Vendors.Domain.Entities;

public class VendorDocument : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VendorId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public string StoragePath { get; set; } = string.Empty; // Guid-based file path
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    
    public DateTime? ExpirationDate { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public string? VerificationComments { get; set; }

    // Navigation
    public virtual Vendor Vendor { get; set; } = null!;
}


