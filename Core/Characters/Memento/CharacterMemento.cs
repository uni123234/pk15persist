using Project.Core.Characters.Models;

namespace Project.Core.Characters.Memento;

/// <summary>
/// [Behavioral Pattern: Memento - Marker Interface]
/// Opaque memento contract preserving encapsulation by hiding internal state from the Caretaker.
/// </summary>
public interface ICharacterMemento
{
    int CharacterId { get; }
    DateTime CreatedAt { get; }
}

/// <summary>
/// [Behavioral Pattern: Memento - Concrete Memento]
/// Captures and encapsulates the full internal state of a Character at a specific moment in time.
/// </summary>
public record CharacterMemento : ICharacterMemento
{
    public int CharacterId { get; init; }
    public string Name { get; init; }
    public int CurrentHealth { get; init; }
    public int BaseHealth { get; init; }
    public int BasePower { get; init; }
    public double BaseResistance { get; init; }
    public List<Item> EquippedItems { get; init; }
    public List<Ability> Abilities { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public CharacterMemento(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        CharacterId = character.Id;
        Name = character.Name;
        CurrentHealth = character.CurrentHealth;
        BaseHealth = character.BaseHealth;
        BasePower = character.BasePower;
        BaseResistance = character.BaseResistance;
        EquippedItems = new List<Item>(character.ActiveItems);
        Abilities = new List<Ability>(character.AvailableAbilities);
    }

    /// <summary>
    /// Restores the encapsulated state into the target character.
    /// </summary>
    public void RestoreTo(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        character.Name = Name;
        character.SetCurrentHealth(CurrentHealth);
        character.ActiveItems.Clear();
        character.ActiveItems.AddRange(EquippedItems);
        character.AvailableAbilities.Clear();
        character.AvailableAbilities.AddRange(Abilities);
    }
}

/// <summary>
/// [Behavioral Pattern: Memento - Caretaker]
/// Manages history stacks of character state mementos, supporting checkpoints and rollbacks (Undo).
/// </summary>
public class CharacterStateCaretaker
{
    private readonly Stack<ICharacterMemento> _history = new();
    private readonly Dictionary<string, ICharacterMemento> _namedCheckpoints = new(StringComparer.OrdinalIgnoreCase);

    public int HistoryCount => _history.Count;

    public void PushSnapshot(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        _history.Push(new CharacterMemento(character));
    }

    public bool Undo(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (_history.Count == 0)
        {
            return false;
        }

        var memento = _history.Pop();
        if (memento is CharacterMemento cm && cm.CharacterId == character.Id)
        {
            cm.RestoreTo(character);
            return true;
        }

        return false;
    }

    public void SaveCheckpoint(string name, Character character)
    {
        _namedCheckpoints[name] = new CharacterMemento(character);
    }

    public bool RestoreCheckpoint(string name, Character character)
    {
        if (_namedCheckpoints.TryGetValue(name, out var memento) && memento is CharacterMemento cm)
        {
            cm.RestoreTo(character);
            return true;
        }
        return false;
    }
}
