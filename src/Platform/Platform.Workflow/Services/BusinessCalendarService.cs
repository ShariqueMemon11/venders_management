using Microsoft.EntityFrameworkCore;
using Platform.Workflow.Interfaces;

namespace Platform.Workflow.Services;

public class BusinessCalendarService : IBusinessCalendarService
{
    private readonly IWorkflowDbContext _context;

    public BusinessCalendarService(IWorkflowDbContext context)
    {
        _context = context;
    }

    public async Task<DateTime> CalculateTargetDateAsync(DateTime startDate, TimeSpan slaDuration, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var targetDate = startDate;
        var remainingDuration = slaDuration;
        
        var holidays = await _context.BusinessHolidays
            .Where(h => h.TenantId == null || h.TenantId == tenantId)
            .Select(h => h.Date.Date)
            .ToListAsync(cancellationToken);

        while (remainingDuration.Ticks > 0)
        {
            var advance = remainingDuration.TotalDays >= 1 ? TimeSpan.FromDays(1) : remainingDuration;
            targetDate = targetDate.Add(advance);
            remainingDuration -= advance;

            while (IsNonBusinessDay(targetDate, holidays))
            {
                targetDate = targetDate.AddDays(1);
            }
        }

        return targetDate;
    }

    private bool IsNonBusinessDay(DateTime date, List<DateTime> holidays)
    {
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            return true;

        if (holidays.Contains(date.Date))
            return true;

        return false;
    }
}