using System.ComponentModel.DataAnnotations;
using GameBackend.Application.Abstractions;
using GameBackend.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Application.Players;

public record PlayerResponse(Guid Id, string Username, string Email, string? AvatarUrl, DateTime CreatedAt);

public record UpdateProfileRequest(
    [Required, StringLength(32, MinimumLength = 3)] string Username,
    [Url, StringLength(512)] string? AvatarUrl);

public class PlayerService(IAppDbContext db)
{
    public async Task<PlayerResponse> GetAsync(Guid playerId, CancellationToken ct)
    {
        return await db.Players
                   .Where(p => p.Id == playerId)
                   .Select(p => new PlayerResponse(p.Id, p.Username, p.Email, p.AvatarUrl, p.CreatedAt))
                   .FirstOrDefaultAsync(ct)
               ?? throw new NotFoundException("Игрок не найден");
    }

    public async Task<PlayerResponse> UpdateAsync(Guid playerId, UpdateProfileRequest request, CancellationToken ct)
    {
        var player = await db.Players.FirstOrDefaultAsync(p => p.Id == playerId, ct)
                     ?? throw new NotFoundException("Игрок не найден");

        if (player.Username != request.Username &&
            await db.Players.AnyAsync(p => p.Username == request.Username, ct))
            throw new ConflictException("Этот ник уже занят");

        player.Username = request.Username;
        player.AvatarUrl = request.AvatarUrl;
        await db.SaveChangesAsync(ct);

        return new PlayerResponse(player.Id, player.Username, player.Email, player.AvatarUrl, player.CreatedAt);
    }
}