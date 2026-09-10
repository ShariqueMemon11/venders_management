namespace Platform.Workflow.Interfaces;

public interface IBusinessCalendarService
{
    Task<DateTime> CalculateTargetDateAsync(DateTime startDate, TimeSpan slaDuration, Guid? tenantId = null, CancellationToken cancellationToken = default);
}