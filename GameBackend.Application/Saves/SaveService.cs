using System.Text;
using System.Text.Json;
using GameBackend.Application.Abstractions;
using GameBackend.Application.Common;
using GameBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Application.Saves;

public class SaveService(IAppDbContext db, TimeProvider timeProvider)
{
    public const int MaxSlots = 3;
    public const int MaxDataBytes = 256 * 1024;

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<SaveSlotSummary>> ListAsync(Guid playerId, CancellationToken ct)
    {
        return await db.SaveSlots
            .Where(s => s.PlayerId == playerId)
            .OrderBy(s => s.SlotNumber)
            .Select(s => new SaveSlotSummary(s.SlotNumber, s.Version, s.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<SaveSlotResponse> GetAsync(Guid playerId, int slot, CancellationToken ct)
    {
        EnsureSlotInRange(slot);

        var save = await db.SaveSlots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.SlotNumber == slot, ct)
            ?? throw new NotFoundException($"Slot {slot} is empty");

        return ToResponse(save);
    }

    public async Task<SaveSlotResponse> PutAsync(
        Guid playerId, int slot, PutSaveRequest request, CancellationToken ct)
    {
        EnsureSlotInRange(slot);

        if (request.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw new ValidationFailedException("The 'data' field is required");

        var raw = request.Data.GetRawText();
        if (Encoding.UTF8.GetByteCount(raw) > MaxDataBytes)
            throw new ValidationFailedException($"Save data exceeds {MaxDataBytes / 1024} KB");

        var save = await db.SaveSlots
            .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.SlotNumber == slot, ct);

        var currentVersion = save?.Version ?? 0;

        if (request.ExpectedVersion is { } expected && expected != currentVersion)
            throw new ConflictException(
                $"Version is outdated: server has {currentVersion}, client has {expected}");

        if (save is null)
        {
            save = new SaveSlot { Id = Guid.NewGuid(), PlayerId = playerId, SlotNumber = slot };
            db.SaveSlots.Add(save);
        }

        save.Data = raw;
        save.Version = currentVersion + 1;
        save.UpdatedAt = Now;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The save was modified concurrently, please retry the request");
        }

        return ToResponse(save);
    }

    public async Task DeleteAsync(Guid playerId, int slot, CancellationToken ct)
    {
        EnsureSlotInRange(slot);

        var save = await db.SaveSlots
            .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.SlotNumber == slot, ct)
            ?? throw new NotFoundException($"Slot {slot} is empty");

        db.SaveSlots.Remove(save);
        await db.SaveChangesAsync(ct);
    }

    private static void EnsureSlotInRange(int slot)
    {
        if (slot is < 1 or > MaxSlots)
            throw new ValidationFailedException($"The slot number must be between 1 and {MaxSlots}");
    }

    private static SaveSlotResponse ToResponse(SaveSlot save) =>
        new(save.SlotNumber, save.Version, save.UpdatedAt, JsonSerializer.Deserialize<JsonElement>(save.Data));
}