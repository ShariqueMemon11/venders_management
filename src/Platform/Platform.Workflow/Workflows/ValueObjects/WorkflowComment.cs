namespace Platform.Workflow.ValueObjects;

public record WorkflowComment
{
    public string Value { get; init; }

    private WorkflowComment(string value)
    {
        Value = value;
    }

    public static WorkflowComment Create(string? value)
    {
        var sanitized = (value ?? string.Empty).Trim();
        if (sanitized.Length > 2000)
            throw new ArgumentException("Comment cannot exceed 2000 characters.");
            
        return new WorkflowComment(sanitized);
    }
}


