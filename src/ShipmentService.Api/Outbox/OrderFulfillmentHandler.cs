using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ShipmentService.Shipments;

namespace ShipmentService.Outbox;

public sealed class OrderFulfillmentHandler : IFulfillmentHandler
{
    public const string HttpClientName = "orders";
    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<OrderFulfillmentHandler> _logger;

    public OrderFulfillmentHandler(string type, IHttpClientFactory httpClients, ILogger<OrderFulfillmentHandler> logger)
    {
        Type = type;
        _httpClients = httpClients;
        _logger = logger;
    }

    public string Type { get; }

    public async Task HandleAsync(FulfillmentNotice notice, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(notice.Payload, out var orderId))
            throw new InvalidOperationException($"Outbox payload is not an order id: {notice.Payload}");

        var fulfillment = Type switch
        {
            FulfillmentNoticeTypes.Pending => nameof(ShipmentStatus.Pending),
            FulfillmentNoticeTypes.Packed => nameof(ShipmentStatus.Packed),
            FulfillmentNoticeTypes.Shipped => nameof(ShipmentStatus.Shipped),
            _ => throw new InvalidOperationException($"No fulfillment for outbox type {Type}.")
        };

        var client = _httpClients.CreateClient(HttpClientName);
        var response = await client.PostAsJsonAsync(
            $"/orders/{orderId}/fulfillment",
            new FulfillmentUpdate(fulfillment),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Processed fulfillment notice {MessageId} {Fulfillment} {OrderId}",
            notice.MessageId,
            fulfillment,
            orderId);
    }

    private sealed record FulfillmentUpdate(string Fulfillment);
}
