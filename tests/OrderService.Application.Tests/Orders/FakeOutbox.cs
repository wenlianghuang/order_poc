using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Tests.Orders;

internal sealed class FakeOutbox : IOutbox
{
    public List<(string Type, string Payload)> Messages { get; } = [];

    public void Add(string type, string payload) => Messages.Add((type, payload));
}
