namespace OrderService.Infrastructure.Outbox;

public interface IOutboxHandler
{
    string Type { get; }

    Task HandleAsync(OutboxNotification notification, CancellationToken cancellationToken);
}
