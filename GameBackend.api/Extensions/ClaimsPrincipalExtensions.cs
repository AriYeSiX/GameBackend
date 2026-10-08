using System.Security.Claims;
using GameBackend.Application.Common;
using Microsoft.IdentityModel.JsonWebTokens;

namespace GameBackend.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetPlayerId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? throw new UnauthorizedException("В токене нет идентификатора игрока"));
}