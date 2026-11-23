using OrderService.Api.Auth;

namespace OrderService.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", (LoginRequest request, AuthOptions auth, JwtTokenIssuer issuer) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.Username))
                errors["username"] = ["Username is required."];

            if (string.IsNullOrWhiteSpace(request.Password))
                errors["password"] = ["Password is required."];

            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var usernameMatches = string.Equals(request.Username, auth.Username, StringComparison.Ordinal);
            var passwordMatches = string.Equals(request.Password, auth.Password, StringComparison.Ordinal);
            if (!usernameMatches || !passwordMatches)
            {
                return Results.Problem(
                    title: "Invalid credentials",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var issued = issuer.Create(auth.Username);
            return Results.Ok(new { token = issued.Token, expiresAtUtc = issued.ExpiresAtUtc });
        });

        return app;
    }
}
