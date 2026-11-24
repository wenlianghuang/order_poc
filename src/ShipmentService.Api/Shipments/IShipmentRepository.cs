namespace ShipmentService.Shipments;

public interface IShipmentRepository
{
    Task<Shipment?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Shipment?> FindByMessageIdAsync(Guid messageId, CancellationToken cancellationToken);
    Task<Shipment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task AddAsync(Shipment shipment, CancellationToken cancellationToken);
    Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken);
    Task<IReadOnlyList<Shipment>> ListAsync(CancellationToken cancellationToken);
}
