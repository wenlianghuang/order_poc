namespace OrderService.Application.Accounts;

public sealed record RegisterCommand(string? Username, string? Password);
