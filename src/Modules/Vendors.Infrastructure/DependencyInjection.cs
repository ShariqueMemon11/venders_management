using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vendors.Application.Interfaces;
using Vendors.Infrastructure.Services;

namespace Vendors.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVendorsModule(this IServiceCollection services)
    {
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IVendorReportService, VendorReportService>();

        return services;
    }
}