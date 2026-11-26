using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShipmentService.Persistence;

namespace ShipmentService.Outbox;

public sealed class FulfillmentOutboxWorker : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<FulfillmentOutboxWorker> _logger;

    public FulfillmentOutboxWorker(IServiceScopeFactory scopes, ILogger<FulfillmentOutboxWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fulfillment outbox worker started.");

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
                _logger.LogError(exception, "Fulfillment outbox processing failed.");
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

    private async Task<FulfillmentBatch> ProcessPendingAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShipmentDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<FulfillmentDispatcher>();
        var pending = await db.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.CreatedAtUtc)
            .Take(20)
            .ToListAsync(stoppingToken);

        if (pending.Count == 0)
            return new FulfillmentBatch(0, 0);

        var notices = pending
            .Select(message => new FulfillmentNotice(message.Id, message.Type, message.Payload))
            .ToArray();
        var processedIds = (await dispatcher.DispatchAsync(notices, stoppingToken)).ToHashSet();
        if (processedIds.Count == 0)
            return new FulfillmentBatch(0, pending.Count);

        var processedAtUtc = DateTime.UtcNow;
        foreach (var message in pending)
        {
            if (processedIds.Contains(message.Id))
                message.ProcessedAtUtc = processedAtUtc;
        }

        await db.SaveChangesAsync(stoppingToken);
        return new FulfillmentBatch(processedIds.Count, pending.Count - processedIds.Count);
    }

    private readonly record struct FulfillmentBatch(int Processed, int Deferred);
}
