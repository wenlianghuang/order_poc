using System.Security.Claims;
using OrderService.Api.Auth;
using OrderService.Application.Orders;
using OrderAppService = OrderService.Application.Orders.OrderService;

namespace OrderService.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/{id:guid}/fulfillment", async (
            Guid id,
            RecordFulfillmentCommand command,
            OrderAppService orders,
            CancellationToken cancellationToken) =>
        {
            var result = await orders.RecordFulfillmentAsync(id, command, cancellationToken);
            return result switch
            {
                RecordFulfillmentResult.Success success => Results.Ok(success.Order),
                RecordFulfillmentResult.InvalidInput invalid => Results.ValidationProblem(invalid.Errors),
                RecordFulfillmentResult.NotFound => Results.NotFound(),
                _ => throw new InvalidOperationException("Unexpected fulfillment result.")
            };
        });

        var group = app.MapGroup("/orders").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            if (!CurrentUserId.TryGet(user, out var ownerId))
                return Results.Unauthorized();

            var list = await orders.ListAsync(ownerId, cancellationToken);
            return Results.Ok(list);
        });

        group.MapPost("/", async (CreateOrderCommand command, ClaimsPrincipal user, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            if (!CurrentUserId.TryGet(user, out var ownerId))
                return Results.Unauthorized();

            var result = await orders.CreateAsync(ownerId, command, cancellationToken);
            return result switch
            {
                CreateOrderResult.Success success => Results.Created($"/orders/{success.Id}", new { id = success.Id }),
                CreateOrderResult.InvalidInput invalid => Results.ValidationProblem(invalid.Errors),
                _ => throw new InvalidOperationException("Unexpected create order result.")
            };
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            if (!CurrentUserId.TryGet(user, out var ownerId))
                return Results.Unauthorized();

            var order = await orders.GetByIdAsync(ownerId, id, cancellationToken);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        group.MapPut("/{id:guid}", async (Guid id, CreateOrderCommand command, ClaimsPrincipal user, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            if (!CurrentUserId.TryGet(user, out var ownerId))
                return Results.Unauthorized();

            var result = await orders.UpdateAsync(ownerId, id, command, cancellationToken);
            return result switch
            {
                UpdateOrderResult.Success success => Results.Ok(success.Order),
                UpdateOrderResult.InvalidInput invalid => Results.ValidationProblem(invalid.Errors),
                UpdateOrderResult.NotFound => Results.NotFound(),
                _ => throw new InvalidOperationException("Unexpected update order result.")
            };
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            if (!CurrentUserId.TryGet(user, out var ownerId))
                return Results.Unauthorized();

            var deleted = await orders.DeleteAsync(ownerId, id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/{id:guid}/pay", async (Guid id, ClaimsPrincipal user, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            if (!CurrentUserId.TryGet(user, out var ownerId))
                return Results.Unauthorized();

            var order = await orders.PayAsync(ownerId, id, cancellationToken);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, ClaimsPrincipal user, OrderAppService orders, CancellationToken cancellationToken) =>
        {
            if (!CurrentUserId.TryGet(user, out var ownerId))
                return Results.Unauthorized();

            var order = await orders.CancelAsync(ownerId, id, cancellationToken);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        return app;
    }
}
