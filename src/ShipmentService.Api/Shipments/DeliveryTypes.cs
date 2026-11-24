namespace ShipmentService.Shipments;

public static class DeliveryTypes
{
    public const string OrderCreated = "OrderCreated";
    public const string OrderPaid = "OrderPaid";
    public const string OrderCancelled = "OrderCancelled";

    public static bool IsKnown(string? type)
        => type is OrderCreated or OrderPaid or OrderCancelled;

    public static bool CreatesShipment(string type) => type == OrderPaid;
}
