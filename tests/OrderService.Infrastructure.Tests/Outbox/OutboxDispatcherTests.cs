using Microsoft.Extensions.Logging.Abstractions;
using OrderService.Infrastructure.Outbox;

namespace OrderService.Infrastructure.Tests.Outbox;

public sealed class OutboxDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_returns_messages_whose_handlers_succeed()
    {
        var created = new RecordingHandler("OrderCreated");
        var paid = new RecordingHandler("OrderPaid");
        var dispatcher = Dispatcher(created, paid);
        var first = Notification("OrderCreated");
        var second = Notification("OrderPaid");

        var processed = await dispatcher.DispatchAsync([first, second], CancellationToken.None);

        Assert.Equal([first.MessageId, second.MessageId], processed);
        Assert.Equal(first, Assert.Single(created.Received));
        Assert.Equal(second, Assert.Single(paid.Received));
    }

    [Fact]
    public async Task DispatchAsync_leaves_an_unknown_type_unprocessed_and_continues()
    {
        var created = new RecordingHandler("OrderCreated");
        var dispatcher = Dispatcher(created);
        var unknown = Notification("OrderShipped");
        var known = Notification("OrderCreated");

        var processed = await dispatcher.DispatchAsync([unknown, known], CancellationToken.None);

        Assert.Equal([known.MessageId], processed);
        Assert.Equal(known, Assert.Single(created.Received));
    }

    [Fact]
    public async Task DispatchAsync_leaves_a_failed_message_unprocessed_and_continues()
    {
        var created = new RecordingHandler("OrderCreated")
        {
            Exception = new InvalidOperationException("delivery failed")
        };
        var paid = new RecordingHandler("OrderPaid");
        var dispatcher = Dispatcher(created, paid);
        var failed = Notification("OrderCreated");
        var succeeded = Notification("OrderPaid");

        var processed = await dispatcher.DispatchAsync([failed, succeeded], CancellationToken.None);

        Assert.Equal([succeeded.MessageId], processed);
        Assert.Equal(failed, Assert.Single(created.Received));
        Assert.Equal(succeeded, Assert.Single(paid.Received));
    }

    [Fact]
    public async Task DispatchAsync_stops_when_the_caller_cancels()
    {
        using var cancellation = new CancellationTokenSource();
        var created = new RecordingHandler("OrderCreated");
        var paid = new RecordingHandler("OrderPaid")
        {
            OnHandle = () =>
            {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            }
        };
        var dispatcher = Dispatcher(created, paid);

        await Assert.ThrowsAsync<OperationCanceledException>(() => dispatcher.DispatchAsync(
            [Notification("OrderCreated"), Notification("OrderPaid"), Notification("OrderCreated")],
            cancellation.Token));

        Assert.Single(created.Received);
        Assert.Single(paid.Received);
    }

    [Fact]
    public void Constructor_rejects_two_handlers_for_the_same_type()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Dispatcher(new RecordingHandler("OrderCreated"), new RecordingHandler("OrderCreated")));

        Assert.Equal("Duplicate outbox handler for OrderCreated.", exception.Message);
    }

    private static OutboxDispatcher Dispatcher(params IOutboxHandler[] handlers)
        => new(handlers, NullLogger<OutboxDispatcher>.Instance);

    private static OutboxNotification Notification(string type)
        => new(Guid.NewGuid(), type, Guid.NewGuid().ToString());

    private sealed class RecordingHandler : IOutboxHandler
    {
        public RecordingHandler(string type) => Type = type;

        public string Type { get; }

        public List<OutboxNotification> Received { get; } = [];

        public Exception? Exception { get; init; }

        public Action? OnHandle { get; init; }

        public Task HandleAsync(OutboxNotification notification, CancellationToken cancellationToken)
        {
            Received.Add(notification);
            OnHandle?.Invoke();
            if (Exception is not null)
                throw Exception;

            return Task.CompletedTask;
        }
    }
}
