using Microsoft.EntityFrameworkCore;
using Vendors.Domain.Entities;

namespace Vendors.Application.Interfaces;

public interface IVendorsDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Vendor> Vendors { get; }
    DbSet<VendorContact> VendorContacts { get; }
    DbSet<VendorAddress> VendorAddresses { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<VendorDocument> VendorDocuments { get; }
    DbSet<VendorContract> VendorContracts { get; }
    DbSet<VendorCompliance> VendorCompliances { get; }
    DbSet<VendorPerformance> VendorPerformances { get; }
    DbSet<VendorRisk> VendorRisks { get; }

    // Temporarily exposed for VendorService queries until CQRS Read Models are fully implemented
    DbSet<global::Platform.Workflow.WorkflowInstance> WorkflowInstances { get; }
    DbSet<global::Platform.Workflow.WorkflowDefinition> WorkflowDefinitions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}