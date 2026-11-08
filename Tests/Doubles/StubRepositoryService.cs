using Project.Infra.Data.Repositories;
using Project.Infra.Data.Serialization;

namespace Tests.Doubles;

/// <summary>
/// [Testing Pattern: Stub]
/// A stub implementation of IRepositoryService that provides canned responses to calls made during tests.
/// Can be configured to return specific saves, return null, or simulate failure scenarios.
/// </summary>
public class StubRepositoryService : IRepositoryService
{
    private readonly Dictionary<string, CharacterSave> _cannedGameSaves = [];
    private readonly Dictionary<string, TextSave> _cannedTextSaves = [];

    public Repository CurrentRepository { get; } = new();

    public bool ThrowOnSave { get; set; }
    public bool ThrowOnLoad { get; set; }

    public void SetupGameSave(string slotName, CharacterSave save)
    {
        _cannedGameSaves[slotName] = save;
    }

    public void SetupTextSave(string slotName, TextSave save)
    {
        _cannedTextSaves[slotName] = save;
    }

    public void SaveGameState(string saveName, CharacterSave save)
    {
        if (ThrowOnSave)
            throw new IOException($"Simulated I/O failure while saving slot '{saveName}'.");

        _cannedGameSaves[saveName] = save;
    }

    public CharacterSave? LoadGameState(string saveName)
    {
        if (ThrowOnLoad)
            throw new InvalidOperationException($"Simulated corruption while loading slot '{saveName}'.");

        return _cannedGameSaves.GetValueOrDefault(saveName);
    }

    public void SaveTextDocument(string saveName, TextSave save)
    {
        if (ThrowOnSave)
            throw new IOException($"Simulated I/O failure while saving text slot '{saveName}'.");

        _cannedTextSaves[saveName] = save;
    }

    public TextSave? LoadTextDocument(string saveName)
    {
        if (ThrowOnLoad)
            throw new InvalidOperationException($"Simulated corruption while loading text slot '{saveName}'.");

        return _cannedTextSaves.GetValueOrDefault(saveName);
    }

    public void SaveToFile(string? filePath = null)
    {
        if (ThrowOnSave)
            throw new IOException("Simulated file write failure.");
    }

    public Repository LoadFromFile(string? filePath = null)
    {
        if (ThrowOnLoad)
            throw new IOException("Simulated file read failure.");

        return CurrentRepository;
    }

    public IEnumerable<string> GetGameSaveNames() => _cannedGameSaves.Keys;

    public IEnumerable<string> GetTextSaveNames() => _cannedTextSaves.Keys;
}
