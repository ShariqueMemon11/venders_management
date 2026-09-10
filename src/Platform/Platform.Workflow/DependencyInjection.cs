using Microsoft.Extensions.DependencyInjection;
using Platform.Workflow.Interfaces;
using Platform.Workflow.Services;

namespace Platform.Workflow;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowPlatform(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowAssignmentResolver, WorkflowAssignmentResolver>();
        services.AddScoped<IBusinessCalendarService, BusinessCalendarService>();
        services.AddScoped<ISlaCalculator, SlaCalculator>();
        services.AddScoped<IWorkflowEngineService, WorkflowEngineService>();

        return services;
    }
}