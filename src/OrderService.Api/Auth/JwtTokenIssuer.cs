using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace OrderService.Api.Auth;

public sealed class JwtTokenIssuer
{
    private readonly AuthOptions _auth;

    public JwtTokenIssuer(AuthOptions auth) => _auth = auth;

    public IssuedToken Create(string username)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_auth.TokenLifetimeMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _auth.Issuer,
            Audience = _auth.Audience,
            Expires = expiresAtUtc,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, username)
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_auth.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new IssuedToken(token, expiresAtUtc);
    }
}
