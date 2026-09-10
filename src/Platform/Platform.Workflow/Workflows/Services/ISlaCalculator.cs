using Platform.Workflow.ValueObjects;

namespace Platform.Workflow.Services;

public interface ISlaCalculator
{
    /// <summary>
    /// Calculates the due date for a given SLA definition from a specified start time.
    /// Takes into account business days, holidays, and working hours if applicable.
    /// </summary>
    Task<DateTime> CalculateDueDateAsync(
        SlaDefinition sla, 
        DateTime startTime, 
        CancellationToken cancellationToken = default);
}


