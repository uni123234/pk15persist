using System.Text.Json.Serialization;

namespace Project.Infra.Data.Serialization;

/// <summary>
/// Abstract base class for persistence save snapshots.
/// </summary>
[JsonDerivedType(typeof(CharacterSave), typeDiscriminator: "CharacterSave")]
[JsonDerivedType(typeof(TextSave), typeDiscriminator: "TextSave")]
public abstract class SaveBase
{
    public string? SaveName { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
