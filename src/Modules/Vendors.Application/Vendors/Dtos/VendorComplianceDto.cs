using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Dtos;

public class VendorComplianceDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string RequirementName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ComplianceStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime? CheckedAt { get; set; }
    public string? CheckedBy { get; set; }
    public string? Comments { get; set; }
}

