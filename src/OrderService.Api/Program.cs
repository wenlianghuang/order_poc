using System.Text.Json;
using OrderService.Api.Auth;
using OrderService.Api.Endpoints;
using OrderService.Api.OpenApi;
using OrderService.Application;
using OrderService.Domain.Exceptions;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Outbox;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddOrderServiceOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOrderServiceOpenApi();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (DomainException exception)
    {
        await Results.Problem(
            title: "Business rule violated",
            detail: exception.Message,
            statusCode: StatusCodes.Status422UnprocessableEntity)
            .ExecuteAsync(context);
    }
    catch (BadHttpRequestException exception) when (exception.InnerException is JsonException jsonException)
    {
        var field = jsonException.Path?.TrimStart('$', '.');
        if (string.IsNullOrWhiteSpace(field))
            field = "body";

        var message = field == "amount"
            ? "Amount must be a number."
            : "The request body is not valid JSON for this command.";

        await Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = [message]
        }).ExecuteAsync(context);
    }
});
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/deploy-check", () => Results.Text("deploy-check: green-dot2", "text/plain"));
app.MapAuthEndpoints();
app.MapOrderEndpoints();

await app.Services.ApplyMigrationsAsync();

app.Run();
