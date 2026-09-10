using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Dtos;

public class VendorRiskDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string RiskCategory { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public string RiskLevelName => RiskLevel.ToString();
    public string? RiskDescription { get; set; }
    public string? MitigationPlan { get; set; }
    public DateTime? LastReviewDate { get; set; }
}

