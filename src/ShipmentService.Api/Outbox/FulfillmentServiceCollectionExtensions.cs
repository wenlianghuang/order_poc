using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShipmentService.Shipments;

namespace ShipmentService.Outbox;

public static class FulfillmentServiceCollectionExtensions
{
    public const string BaseAddressKey = "Orders:BaseAddress";

    public static IServiceCollection AddFulfillmentProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        var baseAddress = configuration[BaseAddressKey];
        if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var address))
            throw new InvalidOperationException($"{BaseAddressKey} is missing or not an absolute URI.");

        services.AddHttpClient(OrderFulfillmentHandler.HttpClientName, client =>
        {
            client.BaseAddress = address;
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        AddHandler(services, FulfillmentNoticeTypes.Pending);
        AddHandler(services, FulfillmentNoticeTypes.Packed);
        AddHandler(services, FulfillmentNoticeTypes.Shipped);
        services.AddScoped<FulfillmentDispatcher>();
        return services;
    }

    private static void AddHandler(IServiceCollection services, string type)
    {
        services.AddSingleton<IFulfillmentHandler>(serviceProvider => new OrderFulfillmentHandler(
            type,
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            serviceProvider.GetRequiredService<ILogger<OrderFulfillmentHandler>>()));
    }
}
