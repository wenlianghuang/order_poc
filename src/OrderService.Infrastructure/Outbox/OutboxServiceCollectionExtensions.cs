using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Application.Orders;

namespace OrderService.Infrastructure.Outbox;

public static class OutboxServiceCollectionExtensions
{
    public const string BaseAddressKey = "Shipments:BaseAddress";

    public static IServiceCollection AddOutboxProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        var baseAddress = configuration[BaseAddressKey];
        if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var address))
            throw new InvalidOperationException($"{BaseAddressKey} is missing or not an absolute URI.");

        services.AddHttpClient(ShippingOutboxHandler.HttpClientName, client =>
        {
            client.BaseAddress = address;
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        AddHandler(services, OutboxMessageTypes.OrderCreated);
        AddHandler(services, OutboxMessageTypes.OrderPaid);
        AddHandler(services, OutboxMessageTypes.OrderCancelled);
        services.AddScoped<OutboxDispatcher>();
        return services;
    }

    private static void AddHandler(IServiceCollection services, string type)
    {
        services.AddSingleton<IOutboxHandler>(serviceProvider => new ShippingOutboxHandler(
            type,
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            serviceProvider.GetRequiredService<ILogger<ShippingOutboxHandler>>()));
    }
}
