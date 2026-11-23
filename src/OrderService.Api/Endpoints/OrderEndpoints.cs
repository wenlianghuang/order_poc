using OrderService.Application.Orders;
using OrderAppService = OrderService.Application.Orders.OrderService;

namespace OrderService.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders");

        group.MapPost("/", async (CreateOrderCommand command, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            var result = await orders.CreateAsync(command, cancellationToken);
            return result switch
            {
                CreateOrderResult.Success success => Results.Created($"/orders/{success.Id}", new { id = success.Id }),
                CreateOrderResult.InvalidInput invalid => Results.ValidationProblem(invalid.Errors),
                _ => throw new InvalidOperationException("Unexpected create order result.")
            };
        }).RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            var order = await orders.GetByIdAsync(id, cancellationToken);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        group.MapPost("/{id:guid}/pay", async (Guid id, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            var order = await orders.PayAsync(id, cancellationToken);
            return order is null ? Results.NotFound() : Results.Ok(order);
        }).RequireAuthorization();

        group.MapPost("/{id:guid}/cancel", async (Guid id, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            var order = await orders.CancelAsync(id, cancellationToken);
            return order is null ? Results.NotFound() : Results.Ok(order);
        }).RequireAuthorization();

        return app;
    }
}
