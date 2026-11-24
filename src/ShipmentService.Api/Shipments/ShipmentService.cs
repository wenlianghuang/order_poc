namespace ShipmentService.Shipments;

public sealed class ShipmentService
{
    private readonly IShipmentRepository _shipments;

    public ShipmentService(IShipmentRepository shipments)
    {
        _shipments = shipments;
    }

    public async Task<AcceptDeliveryResult> AcceptAsync(AcceptDeliveryCommand command, CancellationToken cancellationToken)
    {
        var errors = Validate(command);
        if (errors.Count > 0)
            return new AcceptDeliveryResult.InvalidInput(errors);

        if (!DeliveryTypes.CreatesShipment(command.Type!))
            return new AcceptDeliveryResult.Acknowledged();

        var messageId = command.MessageId!.Value;
        var orderId = command.OrderId!.Value;
        var existing = await FindExistingAsync(messageId, orderId, cancellationToken);
        if (existing is not null)
            return new AcceptDeliveryResult.ShipmentAlreadyExists(ToDto(existing));

        var shipment = Shipment.Create(messageId, orderId);
        try
        {
            await _shipments.AddAsync(shipment, cancellationToken);
        }
        catch (DuplicateShipmentException)
        {
            existing = await FindExistingAsync(messageId, orderId, cancellationToken);
            if (existing is null)
                throw new InvalidOperationException("A shipment for this delivery already exists, but it could not be reloaded.");

            return new AcceptDeliveryResult.ShipmentAlreadyExists(ToDto(existing));
        }

        return new AcceptDeliveryResult.ShipmentCreated(ToDto(shipment));
    }

    public async Task<IReadOnlyList<ShipmentDto>> ListAsync(CancellationToken cancellationToken)
    {
        var shipments = await _shipments.ListAsync(cancellationToken);
        return shipments.Select(ToDto).ToArray();
    }

    public Task<UpdateShipmentResult> PackAsync(Guid id, CancellationToken cancellationToken)
        => AdvanceAsync(id, shipment => shipment.Pack(), cancellationToken);

    public Task<UpdateShipmentResult> ShipAsync(Guid id, CancellationToken cancellationToken)
        => AdvanceAsync(id, shipment => shipment.Ship(), cancellationToken);

    private async Task<UpdateShipmentResult> AdvanceAsync(Guid id, Action<Shipment> advance, CancellationToken cancellationToken)
    {
        var shipment = await _shipments.FindByIdAsync(id, cancellationToken);
        if (shipment is null)
            return new UpdateShipmentResult.NotFound();

        advance(shipment);
        await _shipments.UpdateAsync(shipment, cancellationToken);
        return new UpdateShipmentResult.Success(ToDto(shipment));
    }

    private async Task<Shipment?> FindExistingAsync(Guid messageId, Guid orderId, CancellationToken cancellationToken)
    {
        var byMessage = await _shipments.FindByMessageIdAsync(messageId, cancellationToken);
        if (byMessage is not null)
            return byMessage;

        return await _shipments.FindByOrderIdAsync(orderId, cancellationToken);
    }

    private static Dictionary<string, string[]> Validate(AcceptDeliveryCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (command.MessageId is null || command.MessageId == Guid.Empty)
            errors["messageId"] = ["Message id is required."];

        if (command.OrderId is null || command.OrderId == Guid.Empty)
            errors["orderId"] = ["Order id is required."];

        if (string.IsNullOrWhiteSpace(command.Type))
            errors["type"] = ["Type is required."];
        else if (!DeliveryTypes.IsKnown(command.Type))
            errors["type"] = ["Type must be OrderCreated, OrderPaid, or OrderCancelled."];

        return errors;
    }

    private static ShipmentDto ToDto(Shipment shipment)
        => new(shipment.Id, shipment.OrderId, shipment.Status.ToString(), shipment.CreatedAtUtc);
}
