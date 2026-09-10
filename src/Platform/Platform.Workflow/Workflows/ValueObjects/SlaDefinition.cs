namespace Platform.Workflow.ValueObjects;

public record SlaDefinition(TimeSpan Duration, bool IsBusinessDays)
{
    public static SlaDefinition Standard(int days) => new(TimeSpan.FromDays(days), true);
    public static SlaDefinition None() => new(TimeSpan.Zero, false);
}


