using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;

namespace OrderService.Application.Tests.Orders;

internal sealed class FakeOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders = [];

    public IReadOnlyList<Order> Orders => _orders;

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        _orders.Add(order);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Order>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> matches = _orders.Where(order => order.OwnerId == ownerId).ToArray();
        return Task.FromResult(matches);
    }

    public Task<Order?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_orders.SingleOrDefault(order => order.OwnerId == ownerId && order.Id == id));

    public Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_orders.SingleOrDefault(order => order.Id == id));

    public Task UpdateAsync(Order order, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task DeleteAsync(Order order, CancellationToken cancellationToken)
    {
        _orders.Remove(order);
        return Task.CompletedTask;
    }
}
