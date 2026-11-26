using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShipmentService.Shipments;

namespace ShipmentService.Persistence;

public sealed class ShipmentRepository : IShipmentRepository
{
    private readonly ShipmentDbContext _db;

    public ShipmentRepository(ShipmentDbContext db) => _db = db;

    public Task<Shipment?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => _db.Shipments.FirstOrDefaultAsync(shipment => shipment.Id == id, cancellationToken);

    public Task<Shipment?> FindByMessageIdAsync(Guid messageId, CancellationToken cancellationToken)
        => _db.Shipments.FirstOrDefaultAsync(shipment => shipment.MessageId == messageId, cancellationToken);

    public Task<Shipment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        => _db.Shipments.FirstOrDefaultAsync(shipment => shipment.OrderId == orderId, cancellationToken);

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        await _db.Shipments.AddAsync(shipment, cancellationToken);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            foreach (var entry in _db.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added).ToList())
                entry.State = EntityState.Detached;

            throw new DuplicateShipmentException();
        }
    }

    public Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken)
    {
        _db.Shipments.Update(shipment);
        return _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Shipment>> ListAsync(CancellationToken cancellationToken)
        => await _db.Shipments
            .OrderByDescending(shipment => shipment.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException postgres
           && postgres.SqlState == PostgresErrorCodes.UniqueViolation;
}
