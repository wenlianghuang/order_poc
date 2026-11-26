using Microsoft.EntityFrameworkCore;
using ShipmentService.Shipments;

namespace ShipmentService.Persistence;

public sealed class ShipmentDbContext : DbContext
{
    public ShipmentDbContext(DbContextOptions<ShipmentDbContext> options) : base(options)
    {
    }

    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentOutboxMessage> OutboxMessages => Set<ShipmentOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShipmentDbContext).Assembly);
    }
}
