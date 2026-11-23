namespace OrderService.Application.Common.Interfaces;

public interface IOutbox
{
    // Stages a row on this request's DbContext. The repository SaveChanges commits it with the order.
    void Add(string type, string payload);
}
