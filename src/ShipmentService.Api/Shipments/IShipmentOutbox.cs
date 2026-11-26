namespace ShipmentService.Shipments;

public interface IShipmentOutbox
{
    // Stages a row on this request's DbContext. The repository SaveChanges commits it with the shipment.
    void Add(string type, string payload);
}
