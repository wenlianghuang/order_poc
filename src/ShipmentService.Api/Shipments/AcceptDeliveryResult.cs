namespace ShipmentService.Shipments;

public abstract record AcceptDeliveryResult
{
    public sealed record ShipmentCreated(ShipmentDto Shipment) : AcceptDeliveryResult;

    public sealed record ShipmentAlreadyExists(ShipmentDto Shipment) : AcceptDeliveryResult;

    public sealed record Acknowledged : AcceptDeliveryResult;

    public sealed record InvalidInput(IDictionary<string, string[]> Errors) : AcceptDeliveryResult;
}
