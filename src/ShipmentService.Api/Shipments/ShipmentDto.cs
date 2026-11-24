namespace ShipmentService.Shipments;

public sealed record ShipmentDto(Guid Id, Guid OrderId, string Status, DateTime CreatedAtUtc);
