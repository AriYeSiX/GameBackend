using GameBackend.Api.Extensions;
using GameBackend.Application.Leaderboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameBackend.Api.Controllers;

[ApiController]
[Route("api/leaderboards/{key}")]
[Authorize]
public class LeaderboardsController(LeaderboardService leaderboardService) : ControllerBase
{
    [HttpPost("scores")]
    public async Task<ActionResult<SubmitScoreResponse>> Submit(
        string key, SubmitScoreRequest request, CancellationToken ct)
        => Ok(await leaderboardService.SubmitAsync(key, User.GetPlayerId(), request.Score, ct));

    [HttpGet("top")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<LeaderboardEntry>>> Top(
        string key, [FromQuery] int count = 10, CancellationToken ct = default)
        => Ok(await leaderboardService.GetTopAsync(key, count, ct));

    [HttpGet("me")]
    public async Task<ActionResult<PlayerRankResponse>> Me(string key, CancellationToken ct)
        => Ok(await leaderboardService.GetPlayerRankAsync(key, User.GetPlayerId(), ct));
}