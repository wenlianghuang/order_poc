namespace ShipmentService.Shipments;

public sealed record AcceptDeliveryCommand(Guid? MessageId, Guid? OrderId, string? Type);
