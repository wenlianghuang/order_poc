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

    public async Task<CreateOrderResult> CreateAsync(Guid ownerId, CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var errors = CreateOrderCommandValidator.Validate(command);
        if (errors.Count > 0)
            return new CreateOrderResult.InvalidInput(errors);

        var order = Order.Create(ownerId, command.CustomerName!, command.Amount!.Value);
        _outbox.Add(OutboxMessageTypes.OrderCreated, order.Id.ToString());
        await _orders.AddAsync(order, cancellationToken);
        return new CreateOrderResult.Success(order.Id);
    }

    public async Task<IReadOnlyList<OrderDto>> ListAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var orders = await _orders.ListByOwnerAsync(ownerId, cancellationToken);
        return orders.Select(ToDto).ToArray();
    }

    public async Task<OrderDto?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(ownerId, id, cancellationToken);
        if (order is null)
            return null;

        return ToDto(order);
    }

    public async Task<UpdateOrderResult> UpdateAsync(Guid ownerId, Guid id, CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var errors = CreateOrderCommandValidator.Validate(command);
        if (errors.Count > 0)
            return new UpdateOrderResult.InvalidInput(errors);

        var order = await _orders.GetByIdAsync(ownerId, id, cancellationToken);
        if (order is null)
            return new UpdateOrderResult.NotFound();

        order.ChangeDetails(command.CustomerName!, command.Amount!.Value);
        await _orders.UpdateAsync(order, cancellationToken);
        return new UpdateOrderResult.Success(ToDto(order));
    }

    public async Task<bool> DeleteAsync(Guid ownerId, Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(ownerId, id, cancellationToken);
        if (order is null)
            return false;

        await _orders.DeleteAsync(order, cancellationToken);
        return true;
    }

    public async Task<OrderDto?> PayAsync(Guid ownerId, Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(ownerId, id, cancellationToken);
        if (order is null)
            return null;

        order.Pay();
        _outbox.Add(OutboxMessageTypes.OrderPaid, order.Id.ToString());
        await _orders.UpdateAsync(order, cancellationToken);
        return ToDto(order);
    }

    public async Task<OrderDto?> CancelAsync(Guid ownerId, Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(ownerId, id, cancellationToken);
        if (order is null)
            return null;

        order.Cancel();
        _outbox.Add(OutboxMessageTypes.OrderCancelled, order.Id.ToString());
        await _orders.UpdateAsync(order, cancellationToken);
        return ToDto(order);
    }

    public async Task<RecordFulfillmentResult> RecordFulfillmentAsync(
        Guid id,
        RecordFulfillmentCommand command,
        CancellationToken cancellationToken)
    {
        var errors = RecordFulfillmentCommandValidator.Validate(command);
        if (errors.Count > 0)
            return new RecordFulfillmentResult.InvalidInput(errors);

        var order = await _orders.FindAsync(id, cancellationToken);
        if (order is null)
            return new RecordFulfillmentResult.NotFound();

        if (order.RecordFulfillment(RecordFulfillmentCommandValidator.Parse(command.Fulfillment!)))
            await _orders.UpdateAsync(order, cancellationToken);

        return new RecordFulfillmentResult.Success(ToDto(order));
    }

    private static OrderDto ToDto(Order order)
        => new(
            order.Id,
            order.CustomerName,
            order.Amount,
            order.Status.ToString(),
            order.Fulfillment?.ToString(),
            order.CreatedAtUtc);
}
