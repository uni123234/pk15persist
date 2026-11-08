using Project.Core.Characters.Services;
using Project.Infra.Data.Serialization;

namespace Project.Infra.Data.SaveSystem;

/// <summary>
/// Service contract managing slot-based game state persistence and "Continue from last save" operations.
/// </summary>
public interface IGameSaveManager
{
    /// <summary>
    /// Gets the absolute directory where save files are stored.
    /// </summary>
    string SavesDirectory { get; }

    /// <summary>
    /// Saves the current game state into the specified save slot.
    /// </summary>
    SaveMetadata Save(string slotName, Game game);

    /// <summary>
    /// Loads the game state from the specified save slot.
    /// </summary>
    CharacterSave Load(string slotName);

    /// <summary>
    /// Discovers the most recently saved slot across all save files.
    /// </summary>
    SaveMetadata? GetLatestSave();

    /// <summary>
    /// Restores the game state from the most recent save file and populates the target game instance.
    /// </summary>
    CharacterSave ContinueFromLatest(Game targetGame);

    /// <summary>
    /// Lists all available save slots sorted descending by modification timestamp.
    /// </summary>
    IReadOnlyList<SaveMetadata> ListSaves();

    /// <summary>
    /// Deletes a save slot file.
    /// </summary>
    bool DeleteSave(string slotName);
}
