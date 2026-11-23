namespace OrderService.Api.Auth;

public sealed record IssuedToken(string Token, DateTime ExpiresAtUtc);
