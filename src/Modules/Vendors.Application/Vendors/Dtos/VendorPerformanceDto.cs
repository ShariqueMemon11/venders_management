namespace Vendors.Application.Vendors.Dtos;

public class VendorPerformanceDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string EvaluationPeriod { get; set; } = string.Empty;
    public decimal QualityScore { get; set; }
    public decimal DeliveryScore { get; set; }
    public decimal ResponsivenessScore { get; set; }
    public decimal ComplianceScore { get; set; }
    public decimal AverageScore { get; set; }
    public string? Comments { get; set; }
    public string? Evaluator { get; set; }
}

