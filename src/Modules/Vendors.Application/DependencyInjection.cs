using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Vendors.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddVendorsApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // CQRS handlers for this module (CreateVendor / GetVendorDirectory first slices).
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
