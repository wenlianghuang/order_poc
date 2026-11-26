namespace OrderService.Application.Orders;

public sealed record OrderDto(
    Guid Id,
    string CustomerName,
    decimal Amount,
    string Status,
    string? Fulfillment,
    DateTime CreatedAtUtc);
