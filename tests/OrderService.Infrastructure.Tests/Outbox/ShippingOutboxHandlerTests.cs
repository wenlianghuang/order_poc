using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using OrderService.Infrastructure.Outbox;

namespace OrderService.Infrastructure.Tests.Outbox;

public sealed class ShippingOutboxHandlerTests
{
    [Fact]
    public async Task HandleAsync_posts_the_order_event()
    {
        var notification = new OutboxNotification(Guid.NewGuid(), "OrderPaid", Guid.NewGuid().ToString());
        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var handler = Handler(stub);

        await handler.HandleAsync(notification, CancellationToken.None);

        Assert.NotNull(stub.Request);
        Assert.Equal(HttpMethod.Post, stub.Request.Method);
        Assert.Equal(new Uri("http://localhost:5065/shipments"), stub.Request.RequestUri);
        var json = stub.Body;
        Assert.NotNull(json);
        using var body = JsonDocument.Parse(json);
        Assert.Equal(notification.MessageId, body.RootElement.GetProperty("messageId").GetGuid());
        Assert.Equal(notification.Payload, body.RootElement.GetProperty("orderId").GetGuid().ToString());
        Assert.Equal("OrderPaid", body.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task HandleAsync_accepts_an_existing_shipment_response()
    {
        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var handler = Handler(stub);

        await handler.HandleAsync(Notification(), CancellationToken.None);

        Assert.NotNull(stub.Request);
    }

    [Fact]
    public async Task HandleAsync_accepts_an_acknowledged_delivery()
    {
        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var handler = Handler(stub);

        await handler.HandleAsync(Notification(), CancellationToken.None);

        Assert.NotNull(stub.Request);
    }

    [Fact]
    public async Task HandleAsync_throws_when_the_shipment_service_rejects_the_delivery()
    {
        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var handler = Handler(stub);

        await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(Notification(), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_throws_when_the_payload_is_not_an_order_id()
    {
        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var handler = Handler(stub);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new OutboxNotification(Guid.NewGuid(), "OrderCreated", "not-an-id"), CancellationToken.None));

        Assert.Equal("Outbox payload is not an order id: not-an-id", exception.Message);
        Assert.Null(stub.Request);
    }

    private static ShippingOutboxHandler Handler(StubHandler stub)
    {
        var client = new HttpClient(stub) { BaseAddress = new Uri("http://localhost:5065") };
        return new ShippingOutboxHandler(
            "OrderPaid",
            new SingleClientFactory(client),
            NullLogger<ShippingOutboxHandler>.Instance);
    }

    private static OutboxNotification Notification()
        => new(Guid.NewGuid(), "OrderPaid", Guid.NewGuid().ToString());

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return _respond(request);
        }
    }
}
