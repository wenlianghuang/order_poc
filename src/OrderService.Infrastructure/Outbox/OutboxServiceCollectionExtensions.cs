using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Application.Orders;

namespace OrderService.Infrastructure.Outbox;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxProcessing(this IServiceCollection services)
    {
        AddLoggingHandler(services, OutboxMessageTypes.OrderCreated);
        AddLoggingHandler(services, OutboxMessageTypes.OrderPaid);
        AddLoggingHandler(services, OutboxMessageTypes.OrderCancelled);
        services.AddScoped<OutboxDispatcher>();
        return services;
    }

    private static void AddLoggingHandler(IServiceCollection services, string type)
    {
        services.AddSingleton<IOutboxHandler>(serviceProvider => new LoggingOutboxHandler(
            type,
            serviceProvider.GetRequiredService<ILogger<LoggingOutboxHandler>>()));
    }
}
