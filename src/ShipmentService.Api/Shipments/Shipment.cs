namespace ShipmentService.Shipments;

public sealed class Shipment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid MessageId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Shipment()
    {
    }

    public static Shipment Create(Guid messageId, Guid orderId)
    {
        if (messageId == Guid.Empty)
            throw new ShipmentException("Message id is required.");

        if (orderId == Guid.Empty)
            throw new ShipmentException("Order id is required.");

        return new Shipment
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            OrderId = orderId,
            Status = ShipmentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Pack()
    {
        if (Status == ShipmentStatus.Packed)
            throw new ShipmentException("Shipment is already packed.");

        if (Status != ShipmentStatus.Pending)
            throw new ShipmentException("Only a pending shipment can be packed.");

        Status = ShipmentStatus.Packed;
    }

    public void Ship()
    {
        if (Status == ShipmentStatus.Shipped)
            throw new ShipmentException("Shipment is already shipped.");

        if (Status != ShipmentStatus.Packed)
            throw new ShipmentException("Only a packed shipment can be shipped.");

        Status = ShipmentStatus.Shipped;
    }
}
