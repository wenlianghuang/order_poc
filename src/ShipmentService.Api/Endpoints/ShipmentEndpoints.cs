using ShipmentService.Shipments;
using ShipmentAppService = ShipmentService.Shipments.ShipmentService;

namespace ShipmentService.Endpoints;

public static class ShipmentEndpoints
{
    public static IEndpointRouteBuilder MapShipmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/shipments", async (ShipmentAppService shipments, CancellationToken cancellationToken) =>
        {
            var list = await shipments.ListAsync(cancellationToken);
            return Results.Ok(list);
        });

        app.MapPost("/shipments", async (AcceptDeliveryCommand command, ShipmentAppService shipments, CancellationToken cancellationToken) =>
        {
            var result = await shipments.AcceptAsync(command, cancellationToken);
            return result switch
            {
                AcceptDeliveryResult.ShipmentCreated created => Results.Created(
                    $"/shipments/{created.Shipment.Id}",
                    created.Shipment),
                AcceptDeliveryResult.ShipmentAlreadyExists existing => Results.Ok(existing.Shipment),
                AcceptDeliveryResult.Acknowledged => Results.NoContent(),
                AcceptDeliveryResult.InvalidInput invalid => Results.ValidationProblem(invalid.Errors),
                _ => throw new InvalidOperationException("Unexpected delivery result.")
            };
        });

        app.MapPost("/shipments/{id:guid}/pack", async (Guid id, ShipmentAppService shipments, CancellationToken cancellationToken) =>
        {
            var result = await shipments.PackAsync(id, cancellationToken);
            return result switch
            {
                UpdateShipmentResult.Success success => Results.Ok(success.Shipment),
                UpdateShipmentResult.NotFound => Results.NotFound(),
                _ => throw new InvalidOperationException("Unexpected pack result.")
            };
        });

        app.MapPost("/shipments/{id:guid}/ship", async (Guid id, ShipmentAppService shipments, CancellationToken cancellationToken) =>
        {
            var result = await shipments.ShipAsync(id, cancellationToken);
            return result switch
            {
                UpdateShipmentResult.Success success => Results.Ok(success.Shipment),
                UpdateShipmentResult.NotFound => Results.NotFound(),
                _ => throw new InvalidOperationException("Unexpected ship result.")
            };
        });

        return app;
    }
}
