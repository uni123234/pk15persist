namespace Project.Infra.Data.SaveSystem;

/// <summary>
/// Metadata describing a persisted game save slot, its timestamp, and content statistics.
/// </summary>
public class SaveMetadata
{
    public string SlotName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int CharacterCount { get; set; }
    public int ItemCount { get; set; }
    public int AbilityCount { get; set; }
    public long FileSizeBytes { get; set; }
    public List<string> CharacterNames { get; set; } = [];

    public override string ToString() =>
        $"[Slot: {SlotName}] Characters: {CharacterCount} ({string.Join(", ", CharacterNames)}), Modified: {Timestamp:yyyy-MM-dd HH:mm:ss} UTC, Size: {FileSizeBytes} B";
}
