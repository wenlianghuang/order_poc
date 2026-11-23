namespace OrderService.Application.Orders;

public sealed record CreateOrderCommand(string? CustomerName, decimal? Amount);
