using ShipmentService.Shipments;

namespace ShipmentService.Persistence;

public sealed class ShipmentOutboxWriter : IShipmentOutbox
{
    private readonly ShipmentDbContext _db;

    public ShipmentOutboxWriter(ShipmentDbContext db) => _db = db;

    public void Add(string type, string payload)
    {
        _db.OutboxMessages.Add(new ShipmentOutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
