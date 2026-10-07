using GameBackend.Application.Abstractions;
using GameBackend.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Application.Players;

public record PlayerResponse(Guid Id, string Username, string Email, DateTime CreatedAt);

public class PlayerService(IAppDbContext db)
{
    public async Task<PlayerResponse> GetAsync(Guid playerId, CancellationToken ct)
    {
        return await db.Players
                   .Where(p => p.Id == playerId)
                   .Select(p => new PlayerResponse(p.Id, p.Username, p.Email, p.CreatedAt))
                   .FirstOrDefaultAsync(ct)
               ?? throw new NotFoundException("Игрок не найден");
    }
}