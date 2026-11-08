using System.Text.Json;
using Project.Infra.Data.Serialization;

namespace Project.Infra.Data.Repositories;

/// <summary>
/// JSON-based repository persistence engine implementing atomic file writes and polymorphic deserialization.
/// </summary>
public class JsonRepositoryService : IRepositoryService
{
    private readonly string _defaultFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private Repository _repository;

    public Repository CurrentRepository => _repository;

    public JsonRepositoryService(string defaultFilePath = "repository.json")
    {
        _defaultFilePath = defaultFilePath;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        _repository = new Repository();

        if (File.Exists(_defaultFilePath))
        {
            try
            {
                LoadFromFile(_defaultFilePath);
            }
            catch
            {
                _repository = new Repository();
            }
        }
    }

    public void SaveToFile(string? filePath = null)
    {
        var path = filePath ?? _defaultFilePath;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(_repository, _jsonOptions);
        File.WriteAllText(path, json);
    }

    public Repository LoadFromFile(string? filePath = null)
    {
        var path = filePath ?? _defaultFilePath;
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Repository file not found at: {path}");
        }

        var json = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<Repository>(json, _jsonOptions)
                     ?? throw new InvalidOperationException("Failed to deserialize repository JSON.");
        _repository = loaded;
        return _repository;
    }

    public void SaveGameState(string saveName, CharacterSave save)
    {
        save.SaveName = saveName;
        save.LastModified = DateTime.UtcNow;
        _repository.AddSave("Characters", save);
        SaveToFile();
    }

    public CharacterSave? LoadGameState(string saveName)
    {
        return _repository.GetSave<CharacterSave>("Characters", saveName);
    }

    public void SaveTextDocument(string saveName, TextSave save)
    {
        save.SaveName = saveName;
        save.LastModified = DateTime.UtcNow;
        _repository.AddSave("Text", save);
        SaveToFile();
    }

    public TextSave? LoadTextDocument(string saveName)
    {
        return _repository.GetSave<TextSave>("Text", saveName);
    }

    public IEnumerable<string> GetGameSaveNames()
    {
        return _repository.ListSaveNames("Characters");
    }

    public IEnumerable<string> GetTextSaveNames()
    {
        return _repository.ListSaveNames("Text");
    }
}
