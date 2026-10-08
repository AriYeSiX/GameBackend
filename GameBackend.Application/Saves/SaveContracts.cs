using System.Text.Json;

namespace GameBackend.Application.Saves;

public record SaveSlotSummary(int SlotNumber, int Version, DateTime UpdatedAt);

public record SaveSlotResponse(int SlotNumber, int Version, DateTime UpdatedAt, JsonElement Data);

public record PutSaveRequest(JsonElement Data, int? ExpectedVersion);