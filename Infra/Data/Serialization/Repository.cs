namespace Project.Infra.Data.Serialization;

/// <summary>
/// Top-level repository storing execution states across systems.
/// </summary>
public class Repository
{
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public Dictionary<string, List<SaveBase>> Saves { get; set; } = new();

    public void AddSave(string category, SaveBase save)
    {
        if (!Saves.TryGetValue(category, out var list))
        {
            list = [];
            Saves[category] = list;
        }

        list.RemoveAll(s => s.SaveName == save.SaveName);
        list.Add(save);
        LastModified = DateTime.UtcNow;
    }

    public T? GetSave<T>(string category, string saveName) where T : SaveBase
    {
        if (Saves.TryGetValue(category, out var list))
        {
            return list.OfType<T>().FirstOrDefault(s => s.SaveName == saveName);
        }

        return null;
    }

    public IEnumerable<string> ListSaveNames(string category)
    {
        if (Saves.TryGetValue(category, out var list))
        {
            return list.Select(s => s.SaveName ?? "(unnamed)");
        }

        return [];
    }
}
