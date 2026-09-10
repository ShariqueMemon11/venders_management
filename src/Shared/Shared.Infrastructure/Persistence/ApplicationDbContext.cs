using Microsoft.EntityFrameworkCore;
using Vendors.Application.Interfaces; 
using Shared.Application.Common.Interfaces; 
using Platform.Workflow.Interfaces;
using Shared.Domain.Common;
using Vendors.Domain.Entities;
using System.Reflection;

using Shared.Domain.Audit;
using Shared.Domain.Identity;
using Shared.Domain.Notifications;
using Shared.Infrastructure.Encryption;

namespace Shared.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IVendorsDbContext, IWorkflowDbContext, IAuditDbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IBankFieldEncryptor _bankFieldEncryptor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService,
        IBankFieldEncryptor bankFieldEncryptor) : base(options)
    {
        _currentUserService = currentUserService;
        _bankFieldEncryptor = bankFieldEncryptor;
    }

    /// <summary>
    /// Used by global query filters. Comes from the JWT TenantId claim on authenticated requests;
    /// Guid.Empty only when there is no HTTP user (startup seed / design-time).
    /// </summary>
    public Guid CurrentTenantId => _currentUserService.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    // Audit DbContext
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorContact> VendorContacts => Set<VendorContact>();
    public DbSet<VendorAddress> VendorAddresses => Set<VendorAddress>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<VendorDocument> VendorDocuments => Set<VendorDocument>();
    public DbSet<VendorContract> VendorContracts => Set<VendorContract>();
    public DbSet<VendorCompliance> VendorCompliances => Set<VendorCompliance>();
    public DbSet<VendorPerformance> VendorPerformances => Set<VendorPerformance>();
    public DbSet<VendorRisk> VendorRisks => Set<VendorRisk>();

    public DbSet<global::Platform.Workflow.WorkflowDefinition> WorkflowDefinitions => Set<global::Platform.Workflow.WorkflowDefinition>();
    public DbSet<global::Platform.Workflow.Entities.WorkflowStepDefinition> WorkflowStepDefinitions => Set<global::Platform.Workflow.Entities.WorkflowStepDefinition>();
    public DbSet<global::Platform.Workflow.Entities.WorkflowTransition> WorkflowTransitions => Set<global::Platform.Workflow.Entities.WorkflowTransition>();
    public DbSet<global::Platform.Workflow.Entities.WorkflowCondition> WorkflowConditions => Set<global::Platform.Workflow.Entities.WorkflowCondition>();
    public DbSet<global::Platform.Workflow.WorkflowInstance> WorkflowInstances => Set<global::Platform.Workflow.WorkflowInstance>();
    public DbSet<global::Platform.Workflow.Entities.WorkflowTask> WorkflowTasks => Set<global::Platform.Workflow.Entities.WorkflowTask>();
    public DbSet<global::Platform.Workflow.Entities.WorkflowHistory> WorkflowHistories => Set<global::Platform.Workflow.Entities.WorkflowHistory>();
    public DbSet<global::Platform.Workflow.Entities.WorkflowAttachment> WorkflowAttachments => Set<global::Platform.Workflow.Entities.WorkflowAttachment>();

    // Workflow (Business Calendar)
    public DbSet<global::Platform.Workflow.Entities.BusinessHoliday> BusinessHolidays => Set<global::Platform.Workflow.Entities.BusinessHoliday>();

    public DbSet<Shared.Infrastructure.Outbox.OutboxMessage> OutboxMessages => Set<Shared.Infrastructure.Outbox.OutboxMessage>();
    public DbSet<Shared.Domain.Notifications.AppNotification> Notifications => Set<Shared.Domain.Notifications.AppNotification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Transparent AES-GCM for bank fields (model = plaintext, store = ciphertext).
        // MaxLength must fit Base64(nonce|tag|ciphertext); 50 was only enough for plaintext.
        builder.Entity<BankAccount>(entity =>
        {
            entity.Property(b => b.AccountNumber)
                .HasConversion(new EncryptedBankFieldConverter(_bankFieldEncryptor))
                .HasMaxLength(512);

            entity.Property(b => b.Iban)
                .HasConversion(new EncryptedNullableBankFieldConverter(_bankFieldEncryptor))
                .HasMaxLength(512);
        });

        // Combined soft-delete + tenant filters replace per-entity soft-delete-only filters for ITenantEntity.
        // EF allows one HasQueryFilter per entity — last registration wins.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (!typeof(ITenantEntity).IsAssignableFrom(clr))
                continue;

            if (typeof(AuditableEntity).IsAssignableFrom(clr))
            {
                typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetAuditableTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(clr)
                    .Invoke(this, new object[] { builder });
            }
        }

        // AppNotification is ITenantEntity but not AuditableEntity
        builder.Entity<AppNotification>().HasQueryFilter(n =>
            !n.IsDeleted && n.TenantId == CurrentTenantId);

        base.OnModelCreating(builder);
    }

    private void SetAuditableTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : AuditableEntity, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e =>
            !e.IsDeleted && e.TenantId == CurrentTenantId);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new Shared.Infrastructure.Outbox.ConvertDomainEventsToOutboxMessagesInterceptor());
        base.OnConfiguring(optionsBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedBy = _currentUserService.UserId;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedBy = _currentUserService.UserId;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State != EntityState.Added)
                continue;

            // Prefer an explicit TenantId (seed / service layer). Only fill from the current user when empty.
            if (entry.Entity.TenantId != Guid.Empty)
                continue;

            var tenantId = _currentUserService.TenantId;
            if (tenantId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Cannot assign TenantId for {entry.Entity.GetType().Name}: entity has none and there is no authenticated tenant context.");
            }

            entry.Entity.TenantId = tenantId;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
