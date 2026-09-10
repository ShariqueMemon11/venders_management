namespace Shared.Domain.Notifications;

public enum NotificationType
{
    VendorSubmitted = 1,
    VendorApproved = 2,
    VendorRejected = 3,
    WorkflowAssigned = 4,
    WorkflowCompleted = 5,
    ContractExpiring = 6,
    DocumentExpiring = 7,
    ComplianceIssue = 8,
    RiskAssessmentDue = 9,
    PerformanceReviewDue = 10,
    VendorTerminated = 11,
    System = 99
}

public enum NotificationSeverity
{
    Info = 1,
    Success = 2,
    Warning = 3,
    Critical = 4
}
