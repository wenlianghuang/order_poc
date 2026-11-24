using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace OrderService.Infrastructure.Outbox;

public sealed class ShippingOutboxHandler : IOutboxHandler
{
    public const string HttpClientName = "shipments";
    private readonly IHttpClientFactory _httpClients;
    private readonly ILogger<ShippingOutboxHandler> _logger;

    public ShippingOutboxHandler(string type, IHttpClientFactory httpClients, ILogger<ShippingOutboxHandler> logger)
    {
        Type = type;
        _httpClients = httpClients;
        _logger = logger;
    }

    public string Type { get; }

    public async Task HandleAsync(OutboxNotification notification, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(notification.Payload, out var orderId))
            throw new InvalidOperationException($"Outbox payload is not an order id: {notification.Payload}");

        var client = _httpClients.CreateClient(HttpClientName);
        var response = await client.PostAsJsonAsync(
            "/shipments",
            new ShipmentDelivery(notification.MessageId, orderId, notification.Type),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Processed outbox message {MessageId} {MessageType} {Payload}",
            notification.MessageId,
            notification.Type,
            notification.Payload);
    }

    private sealed record ShipmentDelivery(Guid MessageId, Guid OrderId, string Type);
}
