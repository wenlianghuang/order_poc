using OrderService.Api.Auth;
using OrderService.Application.Accounts;

namespace OrderService.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/register", async (RegisterRequest request, AccountService accounts, CancellationToken cancellationToken) =>
        {
            var result = await accounts.RegisterAsync(new RegisterCommand(request.Username, request.Password), cancellationToken);
            return result switch
            {
                RegisterResult.Success success => Results.Created($"/users/{success.Id}", new { id = success.Id, username = success.Username }),
                RegisterResult.InvalidInput invalid => Results.ValidationProblem(invalid.Errors),
                RegisterResult.UsernameTaken => Results.Problem(
                    title: "Username is already taken.",
                    statusCode: StatusCodes.Status409Conflict),
                _ => throw new InvalidOperationException("Unexpected register result.")
            };
        });

        app.MapPost("/login", async (LoginRequest request, AccountService accounts, JwtTokenIssuer issuer, CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.Username))
                errors["username"] = ["Username is required."];

            if (string.IsNullOrWhiteSpace(request.Password))
                errors["password"] = ["Password is required."];

            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var userId = await accounts.AuthenticateAsync(request.Username!, request.Password!, cancellationToken);
            if (userId is null)
            {
                return Results.Problem(
                    title: "Invalid credentials",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var issued = issuer.Create(userId.Value);
            return Results.Ok(new { token = issued.Token, expiresAtUtc = issued.ExpiresAtUtc });
        });

        return app;
    }
}
