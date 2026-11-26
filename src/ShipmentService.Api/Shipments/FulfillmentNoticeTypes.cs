namespace ShipmentService.Shipments;

public static class FulfillmentNoticeTypes
{
    public const string Pending = "FulfillmentPending";
    public const string Packed = "FulfillmentPacked";
    public const string Shipped = "FulfillmentShipped";

    public static string For(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Pending => Pending,
        ShipmentStatus.Packed => Packed,
        ShipmentStatus.Shipped => Shipped,
        _ => throw new InvalidOperationException($"No fulfillment notice for {status}.")
    };
}
