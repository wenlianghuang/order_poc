using Microsoft.Extensions.DependencyInjection;

namespace OrderService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Orders.OrderService>();
        return services;
    }
}
