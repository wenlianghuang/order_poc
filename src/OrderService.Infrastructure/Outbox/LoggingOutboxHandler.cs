using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.Outbox;

public sealed class LoggingOutboxHandler : IOutboxHandler
{
    private readonly ILogger<LoggingOutboxHandler> _logger;

    public LoggingOutboxHandler(string type, ILogger<LoggingOutboxHandler> logger)
    {
        Type = type;
        _logger = logger;
    }

    public string Type { get; }

    public Task HandleAsync(OutboxNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processed outbox message {MessageId} {MessageType} {Payload}",
            notification.MessageId,
            notification.Type,
            notification.Payload);
        return Task.CompletedTask;
    }
}
