using Project.Infra.Data.Serialization;

namespace Project.Infra.Data.Repositories;

/// <summary>
/// Service abstraction for repository persistence and lifecycle management.
/// </summary>
public interface IRepositoryService
{
    Repository CurrentRepository { get; }
    void SaveToFile(string? filePath = null);
    Repository LoadFromFile(string? filePath = null);
    void SaveGameState(string saveName, CharacterSave save);
    CharacterSave? LoadGameState(string saveName);
    void SaveTextDocument(string saveName, TextSave save);
    TextSave? LoadTextDocument(string saveName);
    IEnumerable<string> GetGameSaveNames();
    IEnumerable<string> GetTextSaveNames();
}
