using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace OrderService.Api.Auth;

public static class JwtAuthentication
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(AuthOptions.SectionName);
        if (!section.Exists())
            throw new InvalidOperationException("Configuration section 'Auth' is missing.");

        var auth = section.Get<AuthOptions>()
            ?? throw new InvalidOperationException("Configuration section 'Auth' is missing.");

        if (string.IsNullOrWhiteSpace(auth.Issuer) || string.IsNullOrWhiteSpace(auth.Audience))
            throw new InvalidOperationException("Auth:Issuer and Auth:Audience are required.");

        if (Encoding.UTF8.GetByteCount(auth.SigningKey) < 32)
            throw new InvalidOperationException("Auth:SigningKey must be at least 32 bytes.");

        if (auth.TokenLifetimeMinutes <= 0)
            throw new InvalidOperationException("Auth:TokenLifetimeMinutes must be greater than zero.");

        services.AddSingleton(auth);
        services.AddSingleton<JwtTokenIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = auth.Issuer,
                    ValidateAudience = true,
                    ValidAudience = auth.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        return services;
    }
}
