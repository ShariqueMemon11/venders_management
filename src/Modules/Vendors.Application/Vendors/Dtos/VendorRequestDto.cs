using Vendors.Domain.Enums;

namespace Vendors.Application.Vendors.Dtos;

public class VendorRequestDto
{
    public Guid Id { get; set; }
    public Guid? VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string RequestNumber { get; set; } = string.Empty;
    public RequestType RequestType { get; set; }
    public string RequestTypeName => RequestType.ToString();
    public RequestStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Requester { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public List<ApprovalStepDto> ApprovalSteps { get; set; } = new();
    public List<RequestHistoryDto> History { get; set; } = new();
}

public class ApprovalStepDto
{
    public Guid Id { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string RequiredRole { get; set; } = string.Empty;
    public string? ApproverUserId { get; set; }
    public ApprovalStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime? ActionDate { get; set; }
    public string? Comments { get; set; }
}

public class RequestHistoryDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

