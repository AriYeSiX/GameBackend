using System.Security.Claims;
using GameBackend.Application.Players;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace GameBackend.api.Controllers;

[ApiController]
[Route("api/players")]
[Authorize]
public class PlayersController(PlayerService playerService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<PlayerResponse>> Me(CancellationToken ct)
    {
        var playerId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        return Ok(await playerService.GetAsync(playerId, ct));
    }
}