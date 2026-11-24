namespace ShipmentService.Shipments;

public sealed class ShipmentException : Exception
{
    public ShipmentException(string message) : base(message)
    {
    }
}
