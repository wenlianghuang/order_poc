using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await _db.Orders.AddAsync(order, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
        => await _db.Orders
            .Where(order => order.OwnerId == ownerId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<Order?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken cancellationToken)
        => _db.Orders.FirstOrDefaultAsync(order => order.Id == id && order.OwnerId == ownerId, cancellationToken);

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken)
    {
        _db.Orders.Update(order);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Order order, CancellationToken cancellationToken)
    {
        _db.Orders.Remove(order);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
