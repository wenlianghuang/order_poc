using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShipmentService.Persistence;

public sealed class ShipmentDbContextFactory : IDesignTimeDbContextFactory<ShipmentDbContext>
{
    public ShipmentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShipmentDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=shipments;Username=postgres;Password=postgres")
            .Options;

        return new ShipmentDbContext(options);
    }
}
