using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.Outbox;

public class ProcessOutboxMessagesJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public ProcessOutboxMessagesJob(IServiceProvider serviceProvider, ILogger<ProcessOutboxMessagesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>(); // MediatR

                var messages = await dbContext.Set<OutboxMessage>()
                    .Where(m => m.ProcessedOnUtc == null)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        var domainEvent = JsonConvert.DeserializeObject<INotification>(
                            message.Content,
                            new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.All
                            });

                        if (domainEvent is null)
                        {
                            _logger.LogWarning("Outbox message {MessageId} could not be deserialized.", message.Id);
                            message.Error = "Deserialization failed.";
                        }
                        else
                        {
                            await publisher.Publish(domainEvent, stoppingToken);
                            message.ProcessedOnUtc = DateTime.UtcNow;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                        message.Error = ex.Message;
                        // Handler SaveChanges may have left an AppNotification (or similar) in Added
                        // with no TenantId. Detach those so we can still persist the outbox Error.
                        DetachNonOutboxPendingEntries(dbContext);
                    }
                }

                if (messages.Any())
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurring while executing outbox job.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private static void DetachNonOutboxPendingEntries(ApplicationDbContext dbContext)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries()
                     .Where(e => e.Entity is not OutboxMessage && e.State != EntityState.Unchanged)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}