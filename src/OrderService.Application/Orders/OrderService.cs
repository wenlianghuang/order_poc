using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;

namespace OrderService.Application.Orders;

public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IOutbox _outbox;

    public OrderService(IOrderRepository orders, IOutbox outbox)
    {
        _orders = orders;
        _outbox = outbox;
    }

    public async Task<CreateOrderResult> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var errors = CreateOrderCommandValidator.Validate(command);
        if (errors.Count > 0)
            return new CreateOrderResult.InvalidInput(errors);

        var order = Order.Create(command.CustomerName!, command.Amount!.Value);
        _outbox.Add(OutboxMessageTypes.OrderCreated, order.Id.ToString());
        await _orders.AddAsync(order, cancellationToken);
        return new CreateOrderResult.Success(order.Id);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return null;

        return ToDto(order);
    }

    public async Task<OrderDto?> PayAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return null;

        order.Pay();
        await _orders.UpdateAsync(order, cancellationToken);
        return ToDto(order);
    }

    public async Task<OrderDto?> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return null;

        order.Cancel();
        await _orders.UpdateAsync(order, cancellationToken);
        return ToDto(order);
    }

    private static OrderDto ToDto(Order order)
        => new(order.Id, order.CustomerName, order.Amount, order.Status.ToString(), order.CreatedAtUtc);
}
