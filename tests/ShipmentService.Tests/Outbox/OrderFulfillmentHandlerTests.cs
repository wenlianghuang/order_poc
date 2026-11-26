using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ShipmentService.Outbox;
using ShipmentService.Shipments;

namespace ShipmentService.Tests.Outbox;

public sealed class OrderFulfillmentHandlerTests
{
    [Fact]
    public async Task HandleAsync_posts_the_fulfillment_to_the_order()
    {
        var orderId = Guid.NewGuid();
        var notice = new FulfillmentNotice(Guid.NewGuid(), FulfillmentNoticeTypes.Packed, orderId.ToString());
        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var handler = Handler(FulfillmentNoticeTypes.Packed, stub);

        await handler.HandleAsync(notice, CancellationToken.None);

        Assert.NotNull(stub.Request);
        Assert.Equal(HttpMethod.Post, stub.Request.Method);
        Assert.Equal(new Uri($"http://localhost:5064/orders/{orderId}/fulfillment"), stub.Request.RequestUri);
        Assert.NotNull(stub.Body);
        using var body = JsonDocument.Parse(stub.Body);
        Assert.Equal(nameof(ShipmentStatus.Packed), body.RootElement.GetProperty("fulfillment").GetString());
    }

    [Fact]
    public async Task HandleAsync_throws_when_the_order_service_rejects_the_notice()
    {
        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var handler = Handler(FulfillmentNoticeTypes.Pending, stub);

        await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(Notice(), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_throws_when_the_payload_is_not_an_order_id()
    {
        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var handler = Handler(FulfillmentNoticeTypes.Shipped, stub);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new FulfillmentNotice(Guid.NewGuid(), FulfillmentNoticeTypes.Shipped, "not-an-id"),
                CancellationToken.None));

        Assert.Equal("Outbox payload is not an order id: not-an-id", exception.Message);
        Assert.Null(stub.Request);
    }

    private static OrderFulfillmentHandler Handler(string type, StubHandler stub)
    {
        var client = new HttpClient(stub) { BaseAddress = new Uri("http://localhost:5064") };
        return new OrderFulfillmentHandler(
            type,
            new SingleClientFactory(client),
            NullLogger<OrderFulfillmentHandler>.Instance);
    }

    private static FulfillmentNotice Notice()
        => new(Guid.NewGuid(), FulfillmentNoticeTypes.Pending, Guid.NewGuid().ToString());

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
