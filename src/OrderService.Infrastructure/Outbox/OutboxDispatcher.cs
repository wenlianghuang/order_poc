using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.Outbox;

public sealed class OutboxDispatcher
{
    private readonly Dictionary<string, IOutboxHandler> _handlers;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IEnumerable<IOutboxHandler> handlers, ILogger<OutboxDispatcher> logger)
    {
        _logger = logger;
        _handlers = new Dictionary<string, IOutboxHandler>(StringComparer.Ordinal);
        foreach (var handler in handlers)
        {
            if (!_handlers.TryAdd(handler.Type, handler))
                throw new InvalidOperationException($"Duplicate outbox handler for {handler.Type}.");
        }
    }

    public async Task<IReadOnlyList<Guid>> DispatchAsync(
        IReadOnlyList<OutboxNotification> pending,
        CancellationToken cancellationToken)
    {
        var processed = new List<Guid>();
        foreach (var notification in pending)
        {
            if (await TryDispatchAsync(notification, cancellationToken))
                processed.Add(notification.MessageId);
        }

        return processed;
    }

    private async Task<bool> TryDispatchAsync(OutboxNotification notification, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(notification.Type, out var handler))
        {
            _logger.LogError(
                "No handler for outbox message {MessageId} {MessageType}",
                notification.MessageId,
                notification.Type);
            return false;
        }

        try
        {
            await handler.HandleAsync(notification, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Outbox handler failed for {MessageId} {MessageType}",
                notification.MessageId,
                notification.Type);
            return false;
        }

        return true;
    }
}
