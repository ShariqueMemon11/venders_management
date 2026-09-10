using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Platform.Workflow;
using Platform.Workflow.Enums;
using Platform.Workflow.Interfaces;
using Platform.Workflow.Repositories;
using Platform.Workflow.Services;
using Platform.Workflow.ValueObjects;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Identity;
using Shared.Infrastructure.Encryption;
using Shared.Infrastructure.Identity;
using Shared.Infrastructure.Persistence;
using Shared.Infrastructure.Persistence.Repositories;
using Vendors.Application.Interfaces;
using Vendors.Domain.Entities;
using Vendors.Infrastructure.Services;

namespace Vendors.Tests.Infrastructure;

/// <summary>
/// Isolated InMemory ApplicationDbContext + VendorService per test run.
/// Seeds the production 2-step vendor workflow (RiskAndCompliance → ProcurementManager final).
/// </summary>
public sealed class VendorTestHost : IAsyncDisposable
{
    public Guid TenantId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public FakeCurrentUserService CurrentUser { get; }
    public string DatabaseName { get; }

    private readonly ServiceProvider _services;

    private VendorTestHost(string databaseName, ServiceProvider services, FakeCurrentUserService currentUser)
    {
        DatabaseName = databaseName;
        _services = services;
        CurrentUser = currentUser;
        CurrentUser.TenantId = TenantId;
        CurrentUser.UserId ??= "procurement@example.com";
        CurrentUser.Role ??= "ProcurementManager";
    }

    public static async Task<VendorTestHost> CreateAsync()
    {
        var databaseName = $"vendors-tests-{Guid.NewGuid():N}";
        var currentUser = new FakeCurrentUserService
        {
            TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UserId = "procurement@example.com",
            Role = "ProcurementManager"
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(currentUser);
        services.AddSingleton<ICurrentUserService>(sp => sp.GetRequiredService<FakeCurrentUserService>());
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddSingleton(new BankDataProtectionSettings
        {
            // Fixed AES-256 test key (32 bytes, Base64) — not for production.
            EncryptionKey = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA="
        });
        services.AddSingleton<IBankFieldEncryptor, AesGcmBankFieldEncryptor>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        services.AddScoped<IVendorsDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkflowDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
        services.AddScoped<IWorkflowAssignmentResolver, WorkflowAssignmentResolver>();
        services.AddScoped<IBusinessCalendarService, BusinessCalendarService>();
        services.AddScoped<ISlaCalculator, SlaCalculator>();
        services.AddScoped<IWorkflowEngineService, WorkflowEngineService>();
        services.AddSingleton<IFileStorageService, NoOpFileStorageService>();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Vendors.Application.DependencyInjection).Assembly));
        services.AddScoped<VendorService>();

        var provider = services.BuildServiceProvider();
        var host = new VendorTestHost(databaseName, provider, currentUser);
        await host.SeedAsync();
        return host;
    }

    public IServiceScope CreateScope() => _services.CreateScope();

    public VendorService GetVendorService(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<VendorService>();

    public ApplicationDbContext GetDb(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    private async Task SeedAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Mirror production seed: no authenticated user while writing explicit TenantIds.
        var previousUser = CurrentUser.UserId;
        var previousRole = CurrentUser.Role;
        var previousTenant = CurrentUser.TenantId;
        CurrentUser.UserId = null;
        CurrentUser.Role = null;
        CurrentUser.TenantId = Guid.Empty;

        db.Tenants.Add(new Tenant
        {
            Id = TenantId,
            Name = "Acme Global Corporation",
            Identifier = "acme-corp",
            IsActive = true,
            CreatedBy = "system-seed"
        });

        var definition = new WorkflowDefinition(
            TenantId,
            "Standard Vendor Onboarding Workflow",
            "A multi-step approval process for new vendor registration.",
            WorkflowEntityType.Vendor);

        var step1 = definition.AddStep(
            "Risk & Compliance Review",
            "Review the risk profile and compliance posture of the vendor.",
            order: 1,
            isFinal: false,
            Assignment.ToRole("RiskAndCompliance"),
            SlaDefinition.Standard(2));

        var step2 = definition.AddStep(
            "Procurement Final Approval",
            "Final review and approval by Procurement Management.",
            order: 2,
            isFinal: true,
            Assignment.ToRole("ProcurementManager"),
            SlaDefinition.Standard(1));

        definition.AddTransition(step1.Id, step2.Id);
        definition.Publish();
        db.WorkflowDefinitions.Add(definition);

        await db.SaveChangesAsync();

        await SeedDemoUsersAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>(), TenantId);

        CurrentUser.UserId = previousUser;
        CurrentUser.Role = previousRole;
        CurrentUser.TenantId = previousTenant == Guid.Empty ? TenantId : previousTenant;
    }

    private static async Task SeedDemoUsersAsync(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        Guid tenantId)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync())
            return;

        var demoUsers = new (string Email, string Password, string Role, string Name)[]
        {
            ("admin@example.com", "admin123", "Admin", "System Admin"),
            ("procurement@example.com", "proc123", "ProcurementManager", "Procurement User"),
            ("risk@example.com", "risk123", "RiskAndCompliance", "Risk & Compliance User"),
            ("viewer@example.com", "view123", "Viewer", "Read-Only User")
        };

        foreach (var (email, password, role, name) in demoUsers)
        {
            db.Users.Add(new ApplicationUser
            {
                Email = email.ToLowerInvariant(),
                PasswordHash = passwordHasher.Hash(password),
                DisplayName = name,
                Role = role,
                TenantId = tenantId,
                IsActive = true,
                CreatedBy = "system-seed"
            });
        }

        await db.SaveChangesAsync();
    }

    public ValueTask DisposeAsync()
    {
        _services.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed class NoOpFileStorageService : IFileStorageService
{
    public Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default) =>
        Task.FromResult($"test/{fileName}");

    public Task<(Stream FileStream, string ContentType, string FileName)> GetFileAsync(string storagePath, CancellationToken cancellationToken = default) =>
        Task.FromResult<(Stream, string, string)>((Stream.Null, "application/octet-stream", Path.GetFileName(storagePath)));

    public Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
