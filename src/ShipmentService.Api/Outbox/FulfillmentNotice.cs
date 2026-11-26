namespace ShipmentService.Outbox;

public sealed record FulfillmentNotice(Guid MessageId, string Type, string Payload);
