namespace OrderService.Application.Orders;

public abstract record CreateOrderResult
{
    public sealed record Success(Guid Id) : CreateOrderResult;

    public sealed record InvalidInput(IDictionary<string, string[]> Errors) : CreateOrderResult;
}
