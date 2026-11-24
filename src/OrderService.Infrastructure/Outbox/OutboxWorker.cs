using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Outbox;

public sealed class OutboxWorker : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(IServiceScopeFactory scopes, ILogger<OutboxWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = await ProcessPendingAsync(stoppingToken);
                if (batch.Processed == 0 || batch.Deferred > 0)
                    await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox processing failed.");
                try
                {
                    await Task.Delay(IdleDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task<OutboxBatch> ProcessPendingAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
        var pending = await db.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.CreatedAtUtc)
            .Take(20)
            .ToListAsync(stoppingToken);

        if (pending.Count == 0)
            return new OutboxBatch(0, 0);

        var notifications = pending
            .Select(message => new OutboxNotification(message.Id, message.Type, message.Payload))
            .ToArray();
        var processedIds = (await dispatcher.DispatchAsync(notifications, stoppingToken)).ToHashSet();
        if (processedIds.Count == 0)
            return new OutboxBatch(0, pending.Count);

        var processedAtUtc = DateTime.UtcNow;
        foreach (var message in pending)
        {
            if (processedIds.Contains(message.Id))
                message.ProcessedAtUtc = processedAtUtc;
        }

        await db.SaveChangesAsync(stoppingToken);
        return new OutboxBatch(processedIds.Count, pending.Count - processedIds.Count);
    }

    private readonly record struct OutboxBatch(int Processed, int Deferred);
}
