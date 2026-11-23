namespace OrderService.Api.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "";

    public string Audience { get; set; } = "";

    public string SigningKey { get; set; } = "";

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public int TokenLifetimeMinutes { get; set; } = 60;
}
