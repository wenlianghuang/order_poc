using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.Persistence;

public sealed class OutboxWriter : IOutbox
{
    private readonly AppDbContext _db;

    public OutboxWriter(AppDbContext db) => _db = db;

    public void Add(string type, string payload)
    {
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
