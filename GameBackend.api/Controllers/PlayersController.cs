using GameBackend.Api.Extensions;
using GameBackend.Application.Players;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameBackend.Api.Controllers;

[ApiController]
[Route("api/players")]
[Authorize]
public class PlayersController(PlayerService playerService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<PlayerResponse>> Me(CancellationToken ct)
        => Ok(await playerService.GetAsync(User.GetPlayerId(), ct));

    [HttpPut("me")]
    public async Task<ActionResult<PlayerResponse>> UpdateMe(UpdateProfileRequest request, CancellationToken ct)
        => Ok(await playerService.UpdateAsync(User.GetPlayerId(), request, ct));
}