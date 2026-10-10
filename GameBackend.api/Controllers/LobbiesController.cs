using GameBackend.Application.Lobbies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameBackend.Api.Controllers;

[ApiController]
[Route("api/lobbies")]
[Authorize]
public class LobbiesController(LobbyService lobbyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LobbyDto>>> List(CancellationToken ct)
        => Ok(await lobbyService.ListOpenAsync(ct));
}