using Shared.Domain.Common;

namespace Platform.Workflow.Entities;

public class WorkflowAttachment : EntityBase
{
    public Guid WorkflowInstanceId { get; private set; }
    public string FileName { get; private set; }
    public string FilePath { get; private set; }
    public string UploadedBy { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private WorkflowAttachment() { } // For EF Core

    internal WorkflowAttachment(string fileName, string filePath, string uploadedBy)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        UploadedBy = uploadedBy ?? throw new ArgumentNullException(nameof(uploadedBy));
        UploadedAt = DateTime.UtcNow;
    }
}



