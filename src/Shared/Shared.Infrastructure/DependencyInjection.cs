using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Common.Interfaces;
using Shared.Infrastructure.Encryption;
using Shared.Infrastructure.Identity;
using Shared.Infrastructure.Persistence;
using Shared.Infrastructure.Storage;

namespace Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var bankProtection = configuration.GetRequiredBankDataProtectionSettings();
        services.AddSingleton(bankProtection);
        services.AddSingleton<IBankFieldEncryptor, AesGcmBankFieldEncryptor>();

        services.AddScoped<Shared.Infrastructure.Audit.AuditInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var outboxInterceptor = new Shared.Infrastructure.Outbox.ConvertDomainEventsToOutboxMessagesInterceptor();
            var auditInterceptor = sp.GetRequiredService<Shared.Infrastructure.Audit.AuditInterceptor>();
            
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .AddInterceptors(auditInterceptor, outboxInterceptor);
        });

        services.AddHostedService<Shared.Infrastructure.Outbox.ProcessOutboxMessagesJob>();

        services.AddScoped<Platform.Workflow.Interfaces.IWorkflowDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<Vendors.Application.Interfaces.IVendorsDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<Shared.Application.Common.Interfaces.IAuditDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        
        services.AddScoped<Platform.Workflow.Repositories.IWorkflowDefinitionRepository, Shared.Infrastructure.Persistence.Repositories.WorkflowDefinitionRepository>();
        services.AddScoped<Platform.Workflow.Repositories.IWorkflowInstanceRepository, Shared.Infrastructure.Persistence.Repositories.WorkflowInstanceRepository>();

        services.AddFileStorage(configuration);
        services.AddScoped<Shared.Application.Notifications.INotificationService, Shared.Infrastructure.Notifications.NotificationService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
