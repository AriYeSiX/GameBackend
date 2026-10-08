using GameBackend.Api.Extensions;
using GameBackend.Application.Saves;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameBackend.Api.Controllers;

[ApiController]
[Route("api/saves")]
[Authorize]
public class SavesController(SaveService saveService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SaveSlotSummary>>> List(CancellationToken ct)
        => Ok(await saveService.ListAsync(User.GetPlayerId(), ct));

    [HttpGet("{slot:int}")]
    public async Task<ActionResult<SaveSlotResponse>> Get(int slot, CancellationToken ct)
        => Ok(await saveService.GetAsync(User.GetPlayerId(), slot, ct));

    [HttpPut("{slot:int}")]
    public async Task<ActionResult<SaveSlotResponse>> Put(int slot, PutSaveRequest request, CancellationToken ct)
        => Ok(await saveService.PutAsync(User.GetPlayerId(), slot, request, ct));

    [HttpDelete("{slot:int}")]
    public async Task<IActionResult> Delete(int slot, CancellationToken ct)
    {
        await saveService.DeleteAsync(User.GetPlayerId(), slot, ct);
        return NoContent();
    }
}