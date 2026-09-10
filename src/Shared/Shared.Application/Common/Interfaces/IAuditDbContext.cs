using Microsoft.EntityFrameworkCore;
using Shared.Domain.Audit;

namespace Shared.Application.Common.Interfaces;

public interface IAuditDbContext
{
    DbSet<AuditEntry> AuditEntries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}