using ShipmentService.Shipments;
using ShipmentAppService = ShipmentService.Shipments.ShipmentService;

namespace ShipmentService.Tests.Shipments;

public sealed class ShipmentServiceTests
{
    private readonly FakeShipmentRepository _shipments = new();
    private readonly ShipmentAppService _service;

    public ShipmentServiceTests()
    {
        _service = new ShipmentAppService(_shipments);
    }

    [Fact]
    public async Task AcceptAsync_creates_a_pending_shipment_when_the_order_is_paid()
    {
        var messageId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var result = await _service.AcceptAsync(Paid(messageId, orderId), CancellationToken.None);

        var created = Assert.IsType<AcceptDeliveryResult.ShipmentCreated>(result);
        Assert.Equal(orderId, created.Shipment.OrderId);
        Assert.Equal(nameof(ShipmentStatus.Pending), created.Shipment.Status);
        Assert.Equal(orderId, Assert.Single(_shipments.Items).OrderId);
    }

    [Theory]
    [InlineData(DeliveryTypes.OrderCreated)]
    [InlineData(DeliveryTypes.OrderCancelled)]
    public async Task AcceptAsync_acknowledges_a_delivery_that_does_not_create_a_shipment(string type)
    {
        var result = await _service.AcceptAsync(
            new AcceptDeliveryCommand(Guid.NewGuid(), Guid.NewGuid(), type),
            CancellationToken.None);

        Assert.IsType<AcceptDeliveryResult.Acknowledged>(result);
        Assert.Empty(_shipments.Items);
    }

    [Fact]
    public async Task AcceptAsync_returns_the_existing_shipment_for_the_same_message()
    {
        var messageId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var command = Paid(messageId, orderId);

        var first = await _service.AcceptAsync(command, CancellationToken.None);
        var second = await _service.AcceptAsync(command, CancellationToken.None);

        var created = Assert.IsType<AcceptDeliveryResult.ShipmentCreated>(first);
        var existing = Assert.IsType<AcceptDeliveryResult.ShipmentAlreadyExists>(second);
        Assert.Equal(created.Shipment.Id, existing.Shipment.Id);
        Assert.Single(_shipments.Items);
    }

    [Fact]
    public async Task AcceptAsync_returns_the_existing_shipment_for_the_same_order()
    {
        var orderId = Guid.NewGuid();
        var first = await _service.AcceptAsync(Paid(Guid.NewGuid(), orderId), CancellationToken.None);
        var second = await _service.AcceptAsync(Paid(Guid.NewGuid(), orderId), CancellationToken.None);

        var created = Assert.IsType<AcceptDeliveryResult.ShipmentCreated>(first);
        var existing = Assert.IsType<AcceptDeliveryResult.ShipmentAlreadyExists>(second);
        Assert.Equal(created.Shipment.Id, existing.Shipment.Id);
        Assert.Single(_shipments.Items);
    }

    [Fact]
    public async Task AcceptAsync_returns_the_existing_shipment_when_insert_hits_a_duplicate()
    {
        var messageId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await _service.AcceptAsync(Paid(messageId, orderId), CancellationToken.None);
        _shipments.BypassFinds = 2;

        var result = await _service.AcceptAsync(Paid(messageId, orderId), CancellationToken.None);

        var existing = Assert.IsType<AcceptDeliveryResult.ShipmentAlreadyExists>(result);
        Assert.Equal(orderId, existing.Shipment.OrderId);
        Assert.Single(_shipments.Items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("OrderShipped")]
    public async Task AcceptAsync_rejects_an_unknown_type(string? type)
    {
        var result = await _service.AcceptAsync(
            new AcceptDeliveryCommand(Guid.NewGuid(), Guid.NewGuid(), type),
            CancellationToken.None);

        var invalid = Assert.IsType<AcceptDeliveryResult.InvalidInput>(result);
        Assert.NotEmpty(invalid.Errors["type"]);
        Assert.Empty(_shipments.Items);
    }

    [Fact]
    public async Task AcceptAsync_rejects_empty_ids()
    {
        var result = await _service.AcceptAsync(
            new AcceptDeliveryCommand(Guid.Empty, Guid.Empty, DeliveryTypes.OrderPaid),
            CancellationToken.None);

        var invalid = Assert.IsType<AcceptDeliveryResult.InvalidInput>(result);
        Assert.Equal("Message id is required.", Assert.Single(invalid.Errors["messageId"]));
        Assert.Equal("Order id is required.", Assert.Single(invalid.Errors["orderId"]));
        Assert.Empty(_shipments.Items);
    }

    [Fact]
    public async Task PackAsync_packs_a_pending_shipment()
    {
        var shipment = await CreatePendingAsync();

        var result = await _service.PackAsync(shipment.Id, CancellationToken.None);

        var success = Assert.IsType<UpdateShipmentResult.Success>(result);
        Assert.Equal(nameof(ShipmentStatus.Packed), success.Shipment.Status);
        Assert.Equal(ShipmentStatus.Packed, _shipments.Items.Single().Status);
    }

    [Fact]
    public async Task PackAsync_rejects_a_packed_shipment_and_leaves_it_packed()
    {
        var shipment = await CreatePendingAsync();
        await _service.PackAsync(shipment.Id, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ShipmentException>(() =>
            _service.PackAsync(shipment.Id, CancellationToken.None));

        Assert.Equal("Shipment is already packed.", exception.Message);
        Assert.Equal(ShipmentStatus.Packed, _shipments.Items.Single().Status);
    }

    [Fact]
    public async Task ShipAsync_rejects_a_pending_shipment_and_leaves_it_pending()
    {
        var shipment = await CreatePendingAsync();

        var exception = await Assert.ThrowsAsync<ShipmentException>(() =>
            _service.ShipAsync(shipment.Id, CancellationToken.None));

        Assert.Equal("Only a packed shipment can be shipped.", exception.Message);
        Assert.Equal(ShipmentStatus.Pending, _shipments.Items.Single().Status);
    }

    [Fact]
    public async Task ShipAsync_ships_a_packed_shipment()
    {
        var shipment = await CreatePendingAsync();
        await _service.PackAsync(shipment.Id, CancellationToken.None);

        var result = await _service.ShipAsync(shipment.Id, CancellationToken.None);

        var success = Assert.IsType<UpdateShipmentResult.Success>(result);
        Assert.Equal(nameof(ShipmentStatus.Shipped), success.Shipment.Status);
        Assert.Equal(ShipmentStatus.Shipped, _shipments.Items.Single().Status);
    }

    [Fact]
    public async Task ShipAsync_rejects_a_shipped_shipment_and_leaves_it_shipped()
    {
        var shipment = await CreatePendingAsync();
        await _service.PackAsync(shipment.Id, CancellationToken.None);
        await _service.ShipAsync(shipment.Id, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ShipmentException>(() =>
            _service.ShipAsync(shipment.Id, CancellationToken.None));

        Assert.Equal("Shipment is already shipped.", exception.Message);
        Assert.Equal(ShipmentStatus.Shipped, _shipments.Items.Single().Status);
    }

    [Fact]
    public async Task PackAsync_returns_not_found_for_an_unknown_shipment()
    {
        var result = await _service.PackAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UpdateShipmentResult.NotFound>(result);
    }

    [Fact]
    public async Task ListAsync_returns_newer_shipments_first()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await _service.AcceptAsync(Paid(Guid.NewGuid(), first), CancellationToken.None);
        await Task.Delay(20);
        await _service.AcceptAsync(Paid(Guid.NewGuid(), second), CancellationToken.None);

        var list = await _service.ListAsync(CancellationToken.None);

        Assert.Equal([second, first], list.Select(shipment => shipment.OrderId));
    }

    private async Task<ShipmentDto> CreatePendingAsync()
    {
        var result = await _service.AcceptAsync(Paid(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        return Assert.IsType<AcceptDeliveryResult.ShipmentCreated>(result).Shipment;
    }

    private static AcceptDeliveryCommand Paid(Guid messageId, Guid orderId)
        => new(messageId, orderId, DeliveryTypes.OrderPaid);

    private sealed class FakeShipmentRepository : IShipmentRepository
    {
        private readonly List<Shipment> _items = [];

        public IReadOnlyList<Shipment> Items => _items;

        public int BypassFinds { get; set; }

        public Task<Shipment?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(_items.SingleOrDefault(shipment => shipment.Id == id));

        public Task<Shipment?> FindByMessageIdAsync(Guid messageId, CancellationToken cancellationToken)
            => FindOrBypass(shipment => shipment.MessageId == messageId);

        public Task<Shipment?> FindByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
            => FindOrBypass(shipment => shipment.OrderId == orderId);

        public Task AddAsync(Shipment shipment, CancellationToken cancellationToken)
        {
            if (_items.Any(item => item.MessageId == shipment.MessageId || item.OrderId == shipment.OrderId))
                throw new DuplicateShipmentException();

            _items.Add(shipment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Shipment>> ListAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<Shipment> items = _items.OrderByDescending(shipment => shipment.CreatedAtUtc).ToArray();
            return Task.FromResult(items);
        }

        private Task<Shipment?> FindOrBypass(Func<Shipment, bool> match)
        {
            if (BypassFinds > 0)
            {
                BypassFinds--;
                return Task.FromResult<Shipment?>(null);
            }

            return Task.FromResult(_items.SingleOrDefault(match));
        }
    }
}
