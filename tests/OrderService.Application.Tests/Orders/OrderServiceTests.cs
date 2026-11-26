using OrderService.Application.Orders;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;
using OrderAppService = OrderService.Application.Orders.OrderService;

namespace OrderService.Application.Tests.Orders;

public sealed class OrderServiceTests
{
    private readonly Guid _ownerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _otherOwnerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly FakeOrderRepository _orders = new();
    private readonly FakeOutbox _outbox = new();
    private readonly OrderAppService _service;

    public OrderServiceTests()
    {
        _service = new OrderAppService(_orders, _outbox);
    }

    [Fact]
    public async Task CreateAsync_saves_the_order_and_stages_order_created()
    {
        var result = await _service.CreateAsync(_ownerId, new CreateOrderCommand("Ada", 120), CancellationToken.None);

        var success = Assert.IsType<CreateOrderResult.Success>(result);
        var order = Assert.Single(_orders.Orders);
        var message = Assert.Single(_outbox.Messages);
        Assert.Equal(success.Id, order.Id);
        Assert.Equal(_ownerId, order.OwnerId);
        Assert.Equal("Ada", order.CustomerName);
        Assert.Equal(120, order.Amount);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(OutboxMessageTypes.OrderCreated, message.Type);
        Assert.Equal(order.Id.ToString(), message.Payload);
    }

    [Fact]
    public async Task CreateAsync_rejects_incomplete_input_without_saving()
    {
        var result = await _service.CreateAsync(_ownerId, new CreateOrderCommand("  ", null), CancellationToken.None);

        var invalid = Assert.IsType<CreateOrderResult.InvalidInput>(result);
        Assert.Equal("Customer name is required.", Assert.Single(invalid.Errors["customerName"]));
        Assert.Equal("Amount is required.", Assert.Single(invalid.Errors["amount"]));
        Assert.Empty(_orders.Orders);
        Assert.Empty(_outbox.Messages);
    }

    [Fact]
    public async Task CreateAsync_does_not_stage_an_outbox_message_when_the_order_is_rejected()
    {
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(_ownerId, new CreateOrderCommand("Ada", 0), CancellationToken.None));

        Assert.Equal("Amount must be greater than zero.", exception.Message);
        Assert.Empty(_orders.Orders);
        Assert.Empty(_outbox.Messages);
    }

    [Fact]
    public async Task ListAsync_returns_only_that_owners_orders()
    {
        var first = await SeedDraft(_ownerId, "Ada");
        await SeedDraft(_otherOwnerId, "Grace");
        var second = await SeedDraft(_ownerId, "Lin");

        var orders = await _service.ListAsync(_ownerId, CancellationToken.None);

        Assert.Collection(
            orders,
            order => Assert.Equal(first.Id, order.Id),
            order => Assert.Equal(second.Id, order.Id));
        Assert.All(orders, order => Assert.Equal("Draft", order.Status));
        var others = await _service.ListAsync(_otherOwnerId, CancellationToken.None);
        Assert.Equal("Grace", Assert.Single(others).CustomerName);
    }

    [Fact]
    public async Task GetByIdAsync_hides_another_owners_order()
    {
        var order = await SeedDraft(_ownerId, "Ada");

        var own = await _service.GetByIdAsync(_ownerId, order.Id, CancellationToken.None);
        var other = await _service.GetByIdAsync(_otherOwnerId, order.Id, CancellationToken.None);

        Assert.NotNull(own);
        Assert.Equal(order.Id, own.Id);
        Assert.Equal("Ada", own.CustomerName);
        Assert.Equal(120, own.Amount);
        Assert.Equal("Draft", own.Status);
        Assert.Null(other);
    }

    [Fact]
    public async Task UpdateAsync_changes_only_that_owners_draft()
    {
        var order = await SeedDraft(_ownerId, "Ada");

        var updated = await _service.UpdateAsync(_ownerId, order.Id, new CreateOrderCommand("Grace", 80), CancellationToken.None);
        var hidden = await _service.UpdateAsync(_otherOwnerId, order.Id, new CreateOrderCommand("Lin", 10), CancellationToken.None);

        var success = Assert.IsType<UpdateOrderResult.Success>(updated);
        Assert.IsType<UpdateOrderResult.NotFound>(hidden);
        Assert.Equal("Grace", success.Order.CustomerName);
        Assert.Equal(80, success.Order.Amount);
        Assert.Equal("Draft", success.Order.Status);
        Assert.Equal("Grace", order.CustomerName);
        Assert.Equal(80, order.Amount);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public async Task UpdateAsync_rejects_incomplete_input_without_changing_the_order()
    {
        var order = await SeedDraft(_ownerId, "Ada");

        var result = await _service.UpdateAsync(_ownerId, order.Id, new CreateOrderCommand("", null), CancellationToken.None);

        var invalid = Assert.IsType<UpdateOrderResult.InvalidInput>(result);
        Assert.Equal("Customer name is required.", Assert.Single(invalid.Errors["customerName"]));
        Assert.Equal("Amount is required.", Assert.Single(invalid.Errors["amount"]));
        Assert.Equal("Ada", order.CustomerName);
        Assert.Equal(120, order.Amount);
    }

    [Fact]
    public async Task DeleteAsync_removes_only_that_owners_order()
    {
        var order = await SeedDraft(_ownerId, "Ada");

        var hidden = await _service.DeleteAsync(_otherOwnerId, order.Id, CancellationToken.None);
        var deleted = await _service.DeleteAsync(_ownerId, order.Id, CancellationToken.None);
        var missing = await _service.DeleteAsync(_ownerId, order.Id, CancellationToken.None);

        Assert.False(hidden);
        Assert.True(deleted);
        Assert.False(missing);
        Assert.Empty(_orders.Orders);
    }

    [Fact]
    public async Task PayAsync_pays_only_that_owners_order()
    {
        var order = await SeedDraft(_ownerId, "Ada");

        var hidden = await _service.PayAsync(_otherOwnerId, order.Id, CancellationToken.None);

        Assert.Null(hidden);
        Assert.Empty(_outbox.Messages);

        var paid = await _service.PayAsync(_ownerId, order.Id, CancellationToken.None);

        Assert.NotNull(paid);
        Assert.Equal("Paid", paid.Status);
        Assert.Equal(OrderStatus.Paid, order.Status);
        var message = Assert.Single(_outbox.Messages);
        Assert.Equal(OutboxMessageTypes.OrderPaid, message.Type);
        Assert.Equal(order.Id.ToString(), message.Payload);
    }

    [Fact]
    public async Task PayAsync_does_not_stage_an_outbox_message_when_payment_is_rejected()
    {
        var order = await SeedDraft(_ownerId, "Ada");
        await _service.PayAsync(_ownerId, order.Id, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _service.PayAsync(_ownerId, order.Id, CancellationToken.None));

        Assert.Equal("Order is already paid.", exception.Message);
        Assert.Equal(OrderStatus.Paid, order.Status);
        var message = Assert.Single(_outbox.Messages);
        Assert.Equal(OutboxMessageTypes.OrderPaid, message.Type);
        Assert.Equal(order.Id.ToString(), message.Payload);
    }

    [Fact]
    public async Task CancelAsync_cancels_only_that_owners_order()
    {
        var order = await SeedDraft(_ownerId, "Ada");

        var hidden = await _service.CancelAsync(_otherOwnerId, order.Id, CancellationToken.None);

        Assert.Null(hidden);
        Assert.Empty(_outbox.Messages);

        var cancelled = await _service.CancelAsync(_ownerId, order.Id, CancellationToken.None);

        Assert.NotNull(cancelled);
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        var message = Assert.Single(_outbox.Messages);
        Assert.Equal(OutboxMessageTypes.OrderCancelled, message.Type);
        Assert.Equal(order.Id.ToString(), message.Payload);
    }

    [Fact]
    public async Task CancelAsync_does_not_stage_an_outbox_message_when_cancellation_is_rejected()
    {
        var order = await SeedDraft(_ownerId, "Ada");
        await _service.PayAsync(_ownerId, order.Id, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _service.CancelAsync(_ownerId, order.Id, CancellationToken.None));

        Assert.Equal("Only a draft order can be cancelled.", exception.Message);
        Assert.Equal(OrderStatus.Paid, order.Status);
        var message = Assert.Single(_outbox.Messages);
        Assert.Equal(OutboxMessageTypes.OrderPaid, message.Type);
        Assert.Equal(order.Id.ToString(), message.Payload);
    }

    [Fact]
    public async Task RecordFulfillmentAsync_records_a_later_status_without_an_order_outbox_message()
    {
        var order = await SeedDraft(_ownerId, "Ada");
        await _service.PayAsync(_ownerId, order.Id, CancellationToken.None);
        _outbox.Messages.Clear();

        var pending = await _service.RecordFulfillmentAsync(
            order.Id,
            new RecordFulfillmentCommand("Pending"),
            CancellationToken.None);
        var shipped = await _service.RecordFulfillmentAsync(
            order.Id,
            new RecordFulfillmentCommand("Shipped"),
            CancellationToken.None);

        Assert.Equal("Pending", Assert.IsType<RecordFulfillmentResult.Success>(pending).Order.Fulfillment);
        var recorded = Assert.IsType<RecordFulfillmentResult.Success>(shipped);
        Assert.Equal("Paid", recorded.Order.Status);
        Assert.Equal("Shipped", recorded.Order.Fulfillment);
        Assert.Equal(FulfillmentStatus.Shipped, order.Fulfillment);
        Assert.Empty(_outbox.Messages);
    }

    [Fact]
    public async Task RecordFulfillmentAsync_keeps_the_later_status_when_an_earlier_notice_arrives()
    {
        var order = await SeedDraft(_ownerId, "Ada");
        order.Pay();
        order.RecordFulfillment(FulfillmentStatus.Shipped);

        var result = await _service.RecordFulfillmentAsync(
            order.Id,
            new RecordFulfillmentCommand("Packed"),
            CancellationToken.None);

        var recorded = Assert.IsType<RecordFulfillmentResult.Success>(result);
        Assert.Equal("Shipped", recorded.Order.Fulfillment);
        Assert.Equal(FulfillmentStatus.Shipped, order.Fulfillment);
    }

    [Fact]
    public async Task RecordFulfillmentAsync_rejects_an_unknown_status_without_changing_the_order()
    {
        var order = await SeedDraft(_ownerId, "Ada");
        order.Pay();

        var result = await _service.RecordFulfillmentAsync(
            order.Id,
            new RecordFulfillmentCommand("Done"),
            CancellationToken.None);

        var invalid = Assert.IsType<RecordFulfillmentResult.InvalidInput>(result);
        Assert.Equal("Fulfillment must be Pending, Packed, or Shipped.", Assert.Single(invalid.Errors["fulfillment"]));
        Assert.Null(order.Fulfillment);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public async Task RecordFulfillmentAsync_returns_not_found_for_an_unknown_order()
    {
        var result = await _service.RecordFulfillmentAsync(
            Guid.NewGuid(),
            new RecordFulfillmentCommand("Pending"),
            CancellationToken.None);

        Assert.IsType<RecordFulfillmentResult.NotFound>(result);
    }

    [Fact]
    public async Task RecordFulfillmentAsync_rejects_a_draft()
    {
        var order = await SeedDraft(_ownerId, "Ada");

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _service.RecordFulfillmentAsync(order.Id, new RecordFulfillmentCommand("Pending"), CancellationToken.None));

        Assert.Equal("Only a paid order can record fulfillment.", exception.Message);
        Assert.Null(order.Fulfillment);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    private async Task<Order> SeedDraft(Guid ownerId, string customerName)
    {
        var order = Order.Create(ownerId, customerName, 120);
        await _orders.AddAsync(order, CancellationToken.None);
        return order;
    }
}
