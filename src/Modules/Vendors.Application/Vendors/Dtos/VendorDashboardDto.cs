namespace Vendors.Application.Vendors.Dtos;

public class VendorDashboardDto
{
    public int TotalVendors { get; set; }
    public int ActiveVendors { get; set; }
    public int PendingApprovalVendors { get; set; }
    public int DraftVendors { get; set; }
    public int TerminatedVendors { get; set; }
    public int TotalComplianceIssues { get; set; }
    public decimal TotalContractValue { get; set; }
    
    public List<PendingApprovalItemDto> PendingApprovals { get; set; } = new();
    public List<PerformanceLeaderboardItemDto> PerformanceLeaderboard { get; set; } = new();
    public List<ComplianceAlertItemDto> ComplianceAlerts { get; set; } = new();
}

public class PendingApprovalItemDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // "Vendor", "Bank Account"
    public string ItemName { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}

public class PerformanceLeaderboardItemDto
{
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string VendorNumber { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ComplianceAlertItemDto
{
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string RequirementName { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty; // "Non-Compliant", "Critical Risk"
    public string Severity { get; set; } = string.Empty; // "High", "Critical", "Medium"
}

