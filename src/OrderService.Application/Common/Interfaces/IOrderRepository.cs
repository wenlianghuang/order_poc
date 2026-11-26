using OrderService.Domain.Entities;

namespace OrderService.Application.Common.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> ListByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);
    Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Order order, CancellationToken cancellationToken);
    Task DeleteAsync(Order order, CancellationToken cancellationToken);
}
