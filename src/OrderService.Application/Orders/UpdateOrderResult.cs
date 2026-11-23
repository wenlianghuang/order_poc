namespace OrderService.Application.Orders;

public abstract record UpdateOrderResult
{
    public sealed record Success(OrderDto Order) : UpdateOrderResult;

    public sealed record InvalidInput(IDictionary<string, string[]> Errors) : UpdateOrderResult;

    public sealed record NotFound : UpdateOrderResult;
}
