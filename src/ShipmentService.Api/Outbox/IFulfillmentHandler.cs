namespace ShipmentService.Outbox;

public interface IFulfillmentHandler
{
    string Type { get; }

    Task HandleAsync(FulfillmentNotice notice, CancellationToken cancellationToken);
}
