using System.Text.Json;
using System.Text.RegularExpressions;
using Project.Core.Characters.Services;
using Project.Infra.Data.Serialization;

namespace Project.Infra.Data.SaveSystem;

/// <summary>
/// File-based game save manager providing slot persistence, atomic file writing, 
/// and "Continue from last save" functionality.
/// </summary>
public class GameSaveManager : IGameSaveManager
{
    private readonly string _savesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public string SavesDirectory => _savesDirectory;

    public GameSaveManager(string? savesDirectory = null)
    {
        _savesDirectory = savesDirectory ?? Path.Combine(AppContext.BaseDirectory, "saves");
        Directory.CreateDirectory(_savesDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public SaveMetadata Save(string slotName, Game game)
    {
        var sanitizedSlot = SanitizeSlotName(slotName);
        var filePath = GetSlotFilePath(sanitizedSlot);

        var saveSnapshot = game.ExportSave(sanitizedSlot);
        saveSnapshot.LastModified = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(saveSnapshot, _jsonOptions);
        WriteTextAtomically(filePath, json);

        var fileInfo = new FileInfo(filePath);
        return new SaveMetadata
        {
            SlotName = sanitizedSlot,
            FilePath = filePath,
            Timestamp = saveSnapshot.LastModified,
            CharacterCount = saveSnapshot.Characters?.Count ?? 0,
            ItemCount = saveSnapshot.Items?.Count ?? 0,
            AbilityCount = saveSnapshot.Abilities?.Count ?? 0,
            FileSizeBytes = fileInfo.Length,
            CharacterNames = saveSnapshot.Characters?.Select(c => c.Name).ToList() ?? []
        };
    }

    public CharacterSave Load(string slotName)
    {
        var sanitizedSlot = SanitizeSlotName(slotName);
        var filePath = GetSlotFilePath(sanitizedSlot);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Save slot '{sanitizedSlot}' not found at: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        var save = JsonSerializer.Deserialize<CharacterSave>(json, _jsonOptions)
                   ?? throw new InvalidOperationException($"Failed to deserialize save slot '{sanitizedSlot}'.");

        return save;
    }

    public SaveMetadata? GetLatestSave()
    {
        var saves = ListSaves();
        return saves.Count > 0 ? saves[0] : null;
    }

    public CharacterSave ContinueFromLatest(Game targetGame)
    {
        var latest = GetLatestSave();
        if (latest == null)
        {
            throw new InvalidOperationException("No existing save games found to continue from.");
        }

        var save = Load(latest.SlotName);
        targetGame.ImportSave(save);
        return save;
    }

    public IReadOnlyList<SaveMetadata> ListSaves()
    {
        if (!Directory.Exists(_savesDirectory))
        {
            return [];
        }

        var result = new List<SaveMetadata>();
        var files = Directory.GetFiles(_savesDirectory, "*.save.json");

        foreach (var file in files)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                var slotName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
                var json = File.ReadAllText(file);
                var save = JsonSerializer.Deserialize<CharacterSave>(json, _jsonOptions);

                if (save != null)
                {
                    result.Add(new SaveMetadata
                    {
                        SlotName = string.IsNullOrWhiteSpace(save.SaveName) ? slotName : save.SaveName,
                        FilePath = file,
                        Timestamp = save.LastModified != default ? save.LastModified : fileInfo.LastWriteTimeUtc,
                        CharacterCount = save.Characters?.Count ?? 0,
                        ItemCount = save.Items?.Count ?? 0,
                        AbilityCount = save.Abilities?.Count ?? 0,
                        FileSizeBytes = fileInfo.Length,
                        CharacterNames = save.Characters?.Select(c => c.Name).ToList() ?? []
                    });
                }
            }
            catch
            {
                // Ignore corrupted or partially written files
            }
        }

        return result.OrderByDescending(s => s.Timestamp).ToList();
    }

    public bool DeleteSave(string slotName)
    {
        var sanitizedSlot = SanitizeSlotName(slotName);
        var filePath = GetSlotFilePath(sanitizedSlot);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }

    private string GetSlotFilePath(string slotName) =>
        Path.Combine(_savesDirectory, $"{slotName}.save.json");

    private static void WriteTextAtomically(string targetPath, string content)
    {
        var dir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tempPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(tempPath, content);

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        File.Move(tempPath, targetPath);
    }

    private static string SanitizeSlotName(string name)
    {
        var trimmed = name.Trim();
        var sanitized = Regex.Replace(trimmed, @"[^a-zA-Z0-9_-]", "_");
        return string.IsNullOrWhiteSpace(sanitized) ? "save_" + DateTime.UtcNow.Ticks : sanitized;
    }
}
