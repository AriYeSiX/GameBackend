namespace GameBackend.Domain.Entities;

public class SaveSlot
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public int SlotNumber { get; set; }
    public string Data { get; set; } = "{}";
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}