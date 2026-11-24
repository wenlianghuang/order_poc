namespace OrderService.Infrastructure.Outbox;

public sealed record OutboxNotification(Guid MessageId, string Type, string Payload);
