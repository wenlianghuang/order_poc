using Microsoft.Extensions.Logging;

namespace ShipmentService.Outbox;

public sealed class FulfillmentDispatcher
{
    private readonly Dictionary<string, IFulfillmentHandler> _handlers;
    private readonly ILogger<FulfillmentDispatcher> _logger;

    public FulfillmentDispatcher(IEnumerable<IFulfillmentHandler> handlers, ILogger<FulfillmentDispatcher> logger)
    {
        _logger = logger;
        _handlers = new Dictionary<string, IFulfillmentHandler>(StringComparer.Ordinal);
        foreach (var handler in handlers)
        {
            if (!_handlers.TryAdd(handler.Type, handler))
                throw new InvalidOperationException($"Duplicate fulfillment handler for {handler.Type}.");
        }
    }

    public async Task<IReadOnlyList<Guid>> DispatchAsync(
        IReadOnlyList<FulfillmentNotice> pending,
        CancellationToken cancellationToken)
    {
        var processed = new List<Guid>();
        foreach (var notice in pending)
        {
            if (await TryDispatchAsync(notice, cancellationToken))
                processed.Add(notice.MessageId);
        }

        return processed;
    }

    private async Task<bool> TryDispatchAsync(FulfillmentNotice notice, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(notice.Type, out var handler))
        {
            _logger.LogError(
                "No handler for fulfillment notice {MessageId} {MessageType}",
                notice.MessageId,
                notice.Type);
            return false;
        }

        try
        {
            await handler.HandleAsync(notice, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Fulfillment handler failed for {MessageId} {MessageType}",
                notice.MessageId,
                notice.Type);
            return false;
        }

        return true;
    }
}
