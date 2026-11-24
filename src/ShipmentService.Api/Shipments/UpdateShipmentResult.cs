namespace ShipmentService.Shipments;

public abstract record UpdateShipmentResult
{
    public sealed record Success(ShipmentDto Shipment) : UpdateShipmentResult;

    public sealed record NotFound : UpdateShipmentResult;
}
