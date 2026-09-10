using Platform.Workflow.Interfaces;
using Platform.Workflow.ValueObjects;

namespace Platform.Workflow.Services;

public class SlaCalculator : ISlaCalculator
{
    private readonly IBusinessCalendarService _businessCalendarService;

    public SlaCalculator(IBusinessCalendarService businessCalendarService)
    {
        _businessCalendarService = businessCalendarService;
    }

    public async Task<DateTime> CalculateDueDateAsync(
        SlaDefinition sla, 
        DateTime startTime, 
        CancellationToken cancellationToken = default)
    {
        if (sla.Duration == TimeSpan.Zero)
        {
            return DateTime.MaxValue;
        }

        if (!sla.IsBusinessDays)
        {
            return startTime.Add(sla.Duration);
        }

        return await _businessCalendarService.CalculateTargetDateAsync(startTime, sla.Duration, null, cancellationToken);
    }
}


