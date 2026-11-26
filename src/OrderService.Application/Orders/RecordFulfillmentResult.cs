namespace OrderService.Application.Orders;

public abstract record RecordFulfillmentResult
{
    public sealed record Success(OrderDto Order) : RecordFulfillmentResult;

    public sealed record InvalidInput(IDictionary<string, string[]> Errors) : RecordFulfillmentResult;

    public sealed record NotFound : RecordFulfillmentResult;
}
