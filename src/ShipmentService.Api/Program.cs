using Microsoft.EntityFrameworkCore;
using ShipmentService.Endpoints;
using ShipmentService.Outbox;
using ShipmentService.Persistence;
using ShipmentService.Shipments;
using ShipmentAppService = ShipmentService.Shipments.ShipmentService;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<ShipmentDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IShipmentOutbox, ShipmentOutboxWriter>();
builder.Services.AddScoped<ShipmentAppService>();
builder.Services.AddFulfillmentProcessing(builder.Configuration);
builder.Services.AddHostedService<FulfillmentOutboxWorker>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "ShipmentService";
        options.SwaggerEndpoint("/openapi/v1.json", "ShipmentService v1");
    });
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ShipmentException exception)
    {
        await Results.Problem(
            title: "Business rule violated",
            detail: exception.Message,
            statusCode: StatusCodes.Status422UnprocessableEntity)
            .ExecuteAsync(context);
    }
});

app.MapShipmentEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShipmentDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
