using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace OrderService.Api.Auth;

public static class CurrentUserId
{
    public static bool TryGet(ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(value, out userId);
    }
}
