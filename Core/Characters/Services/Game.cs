using Project.Core.Characters.Combat;
using Project.Core.Characters.CombatPipeline;
using Project.Core.Characters.Mediator;
using Project.Core.Characters.Memento;
using Project.Core.Characters.Models;
using Project.Core.Characters.Observer;
using Project.Core.Characters.Strategies;
using Project.Core.Characters.TurnBased;
using Project.Core.Characters.ViewModels;
using Project.Infra.Caching;
using Project.Infra.Data.Models;
using Project.Infra.Data.SaveSystem;
using Project.Infra.Data.Serialization;
using Project.Infra.Display;
using Project.Infra.Display.DTOs;
using Project.Infra.Remote.Models;

namespace Project.Core.Characters.Services;

/// <summary>
/// Domain facade governing character interactions, inventory, combat mechanics, and save state.
/// Incorporates Behavioral Patterns: Mediator, Observer, Memento, Chain of Responsibility, Strategy.
/// </summary>
public class Game
{
    private IDisplayer _displayer;
    private readonly List<Character> _characters = [];
    private readonly List<Item> _items = [];
    private readonly List<Ability> _abilities = [];
    private int _nextCharId = 1;
    private int _nextItemId = 1;
    private int _nextAbilityId = 1;

    // Behavioral Pattern & Business Logic components
    private readonly BattleMediator _mediator = new();
    private readonly CharacterStateCaretaker _caretaker = new();
    private readonly CombatStatsObserver _statsObserver = new();
    private readonly TurnBasedBattleEngine _battleEngine;
    private CombatLogObserver _logObserver;

    public GameConfig Config { get; set; } = new();
    public IBattleMediator Mediator => _mediator;
    public CharacterStateCaretaker Caretaker => _caretaker;
    public CombatStatsObserver StatsObserver => _statsObserver;
    public TurnBasedBattleEngine BattleEngine => _battleEngine;
    public BattleHistory History => _battleEngine.History;
    public IAssetCacheService PortraitCache { get; set; } = new LocalFileAssetCacheService();
    public IGameSaveManager SaveManager { get; set; } = new GameSaveManager();

    public Game(IDisplayer displayer)
    {
        _displayer = displayer;
        _logObserver = new CombatLogObserver(_displayer);
        _mediator.SubscribeObserver(_logObserver);
        _mediator.SubscribeObserver(_statsObserver);
        _battleEngine = new TurnBasedBattleEngine(Config);
    }

    public SaveMetadata SaveGame(string slotName = "latest")
    {
        return SaveManager.Save(slotName, this);
    }

    public CharacterSave LoadGame(string slotName)
    {
        var save = SaveManager.Load(slotName);
        ImportSave(save);
        return save;
    }

    public CharacterSave ContinueFromLatest()
    {
        return SaveManager.ContinueFromLatest(this);
    }

    public IReadOnlyList<SaveMetadata> ListSaves()
    {
        return SaveManager.ListSaves();
    }

    public void SetDisplayer(IDisplayer newDisplayer)
    {
        _mediator.UnsubscribeObserver(_logObserver);
        _displayer = newDisplayer;
        _logObserver = new CombatLogObserver(_displayer);
        _mediator.SubscribeObserver(_logObserver);
    }

    public IReadOnlyList<Character> Characters => _characters.AsReadOnly();
    public IReadOnlyList<Item> Items => _items.AsReadOnly();
    public IReadOnlyList<Ability> Abilities => _abilities.AsReadOnly();

    public Character CreateCharacter(string name,
                                     int health,
                                     int power,
                                     double resistance,
                                     IEnumerable<Ability>? abilities = null,
                                     IEnumerable<Item>? items = null,
                                     int armor = 0,
                                     int speed = 100,
                                     double resistPenetration = 0.0)
    {
        var id = _nextCharId++;
        var newChar = new Character(id, name, health, power, resistance, armor, speed, resistPenetration);

        if (abilities != null)
        {
            foreach (var ability in abilities)
                newChar.AddAbility(ability);
        }

        if (items != null)
        {
            foreach (var item in items)
                newChar.AddItem(item);
        }

        _characters.Add(newChar);
        _mediator.RegisterCharacter(newChar);

        _displayer.Display("New character was created:");
        _displayer.Display(new CharacterDto(newChar));
        return newChar;
    }

    public Item CreateItem(string name, double hpMod, double powerMod, double armorMod = 0, double speedMod = 0)
    {
        var item = new Item(_nextItemId++, name, hpMod, powerMod, armorMod, speedMod);
        _items.Add(item);
        _displayer.Display($"Item created: {new ItemDto(item).Render()}");
        return item;
    }

    public Ability CreateAbility(string name, double damage, double heal, string description)
    {
        var ability = new Ability(_nextAbilityId++, name, damage, heal, description);
        _abilities.Add(ability);
        _displayer.Display($"Ability created: {new AbilityDto(ability).Render()}");
        return ability;
    }

    public bool EquipItem(string charIdOrName, string itemIdOrName)
    {
        var character = FindCharacter(charIdOrName);
        if (character == null)
        {
            _displayer.Display($"Error: Character '{charIdOrName}' not found.");
            return false;
        }

        var item = FindItem(itemIdOrName);
        if (item == null)
        {
            _displayer.Display($"Error: Item '{itemIdOrName}' not found.");
            return false;
        }

        character.AddItem(item);
        _displayer.Display($"Equipped item '{item.Name}' to '{character.Name}'.");
        _displayer.Display(new CharacterDto(character));
        return true;
    }

    public bool LearnAbility(string charIdOrName, string abilityIdOrName)
    {
        var character = FindCharacter(charIdOrName);
        if (character == null)
        {
            _displayer.Display($"Error: Character '{charIdOrName}' not found.");
            return false;
        }

        var ability = FindAbility(abilityIdOrName);
        if (ability == null)
        {
            _displayer.Display($"Error: Ability '{abilityIdOrName}' not found.");
            return false;
        }

        character.AddAbility(ability);
        _displayer.Display($"Character '{character.Name}' learned ability '{ability.Name}'.");
        _displayer.Display(new CharacterDto(character));
        return true;
    }

    /// <summary>
    /// Executes standard or customized attack using Mediator, Strategy, and Chain of Responsibility.
    /// </summary>
    public void Attack(string actorIdOrName, string targetIdOrName, IAttackStrategy? strategy = null)
    {
        var actor = FindCharacter(actorIdOrName);
        var target = FindCharacter(targetIdOrName);

        if (actor == null || target == null)
        {
            _displayer.Display("Error: Both actor and target characters must exist.");
            return;
        }

        if (!actor.IsAlive)
        {
            _displayer.Display($"Action failed: {actor.Name} is defeated.");
            return;
        }

        if (!target.IsAlive)
        {
            _displayer.Display($"Action failed: {target.Name} is already defeated.");
            return;
        }

        ExecuteAttack(actor, target, strategy);
    }

    /// <summary>
    /// Direct attack execution delegating to the BattleMediator.
    /// </summary>
    public void ExecuteAttack(Character attacker, Character target, IAttackStrategy? strategy = null)
    {
        _mediator.ExecuteAttack(attacker, target, strategy);
    }

    public void Heal(string actorIdOrName, string targetIdOrName)
    {
        var actor = FindCharacter(actorIdOrName);
        var target = FindCharacter(targetIdOrName);

        if (actor == null || target == null)
        {
            _displayer.Display("Error: Both actor and target characters must exist.");
            return;
        }

        if (!actor.IsAlive)
        {
            _displayer.Display($"Action failed: {actor.Name} is defeated.");
            return;
        }

        var healAmount = (int)Math.Round(actor.Power * 0.75);
        var effectiveHeal = target.ReceiveHealing(healAmount);
        _displayer.Display($"{actor.Name} heals {target.Name} for +{effectiveHeal} HP! (Current HP: {target.Health}/{target.MaxHealth})");
    }

    public void UseAbility(string actorIdOrName, string targetIdOrName, string abilityIdOrName)
    {
        var actor = FindCharacter(actorIdOrName);
        var target = FindCharacter(targetIdOrName);

        if (actor == null || target == null)
        {
            _displayer.Display("Error: Both actor and target characters must exist.");
            return;
        }

        if (!actor.IsAlive)
        {
            _displayer.Display($"Action failed: {actor.Name} is defeated.");
            return;
        }

        var ability = actor.AvailableAbilities.FirstOrDefault(a =>
            a.Id.ToString() == abilityIdOrName || a.Name.Equals(abilityIdOrName, StringComparison.OrdinalIgnoreCase))
            ?? FindAbility(abilityIdOrName);

        if (ability == null)
        {
            _displayer.Display($"Error: Ability '{abilityIdOrName}' is not available to {actor.Name}.");
            return;
        }

        _mediator.ExecuteAbility(actor, target, ability);
    }

    /// <summary>
    /// Executes a full turn-based duel between two characters using Speed timeline and mathematical combat formulas.
    /// </summary>
    public BattleSimulationResult StartTurnBasedDuel(string fighter1IdOrName, string fighter2IdOrName)
    {
        var f1 = FindCharacter(fighter1IdOrName) ?? throw new InvalidOperationException($"Fighter '{fighter1IdOrName}' not found.");
        var f2 = FindCharacter(fighter2IdOrName) ?? throw new InvalidOperationException($"Fighter '{fighter2IdOrName}' not found.");

        _displayer.Display($"=== Starting Turn-Based Battle: {f1.Name} vs {f2.Name} ===");
        var result = _battleEngine.RunBattle(f1, f2, turn =>
        {
            _displayer.Display($"  [R{turn.RoundNumber} Turn #{turn.TurnNumber}] {turn.LogMessage}");
        });

        _displayer.Display($"=== Battle Concluded! Winner: {result.Winner.Name} in {result.TotalRounds} rounds ({result.TotalTurns} turns) ===");
        return result;
    }

    /// <summary>
    /// Executes an individual turn action in the turn-based engine and logs to History.
    /// </summary>
    public Turn ExecuteTurn(int round, string actorIdOrName, string targetIdOrName, TurnActionType action, string? abilityIdOrName = null)
    {
        var actor = FindCharacter(actorIdOrName) ?? throw new InvalidOperationException($"Actor '{actorIdOrName}' not found.");
        var target = FindCharacter(targetIdOrName) ?? throw new InvalidOperationException($"Target '{targetIdOrName}' not found.");

        Ability? ability = null;
        if (!string.IsNullOrEmpty(abilityIdOrName))
        {
            ability = actor.AvailableAbilities.FirstOrDefault(a => a.Name.Equals(abilityIdOrName, StringComparison.OrdinalIgnoreCase))
                   ?? FindAbility(abilityIdOrName);
        }

        var isCrit = (actor.Power % 3 == 0);
        var turn = _battleEngine.ExecuteTurn(round, actor, target, action, ability, isCrit);
        _displayer.Display($"  {turn.LogMessage}");
        return turn;
    }

    /// <summary>
    /// Formats a complete presentation ViewModel for GUI/CLI presentation.
    /// </summary>
    public CharacterProfileViewModel GetProfileViewModel(string charIdOrName, RemoteCharacterData? remoteData = null)
    {
        var ch = FindCharacter(charIdOrName) ?? throw new InvalidOperationException($"Character '{charIdOrName}' not found.");
        return new CharacterProfileViewModel(ch, remoteData);
    }

    /// <summary>
    /// [Memento Pattern] Creates an encapsulated state snapshot for the target character.
    /// </summary>
    public void CreateSnapshot(string characterIdOrName, string? checkpointName = null)
    {
        var ch = FindCharacter(characterIdOrName);
        if (ch == null)
        {
            _displayer.Display($"Error: Character '{characterIdOrName}' not found.");
            return;
        }

        if (string.IsNullOrWhiteSpace(checkpointName))
        {
            _caretaker.PushSnapshot(ch);
            _displayer.Display($"[Memento] Snapshot created for {ch.Name} (HP: {ch.Health}, Power: {ch.Power}). History depth: {_caretaker.HistoryCount}.");
        }
        else
        {
            _caretaker.SaveCheckpoint(checkpointName, ch);
            _displayer.Display($"[Memento] Named checkpoint '{checkpointName}' saved for {ch.Name}.");
        }
    }

    /// <summary>
    /// [Memento Pattern] Restores character state from either undo history stack or named checkpoint.
    /// </summary>
    public bool RestoreSnapshot(string characterIdOrName, string? checkpointName = null)
    {
        var ch = FindCharacter(characterIdOrName);
        if (ch == null)
        {
            _displayer.Display($"Error: Character '{characterIdOrName}' not found.");
            return false;
        }

        bool success = string.IsNullOrWhiteSpace(checkpointName)
            ? _caretaker.Undo(ch)
            : _caretaker.RestoreCheckpoint(checkpointName, ch);

        if (success)
        {
            _displayer.Display($"[Memento] Successfully restored state for {ch.Name} (HP: {ch.Health}/{ch.MaxHealth}, Power: {ch.Power}).");
        }
        else
        {
            _displayer.Display($"[Memento] Failed to restore snapshot for {ch.Name}. No valid checkpoint found.");
        }

        return success;
    }

    public void ListCharacters(string? idOrName = null)
    {
        if (string.IsNullOrWhiteSpace(idOrName))
        {
            _displayer.Display("=== Characters List ===");
            if (_characters.Count == 0)
            {
                _displayer.Display("  (No characters registered)");
                return;
            }

            foreach (var ch in _characters)
            {
                var status = ch.IsAlive ? $"HP: {ch.Health}/{ch.MaxHealth}" : "DEFEATED";
                _displayer.Display($"  #{ch.Id} {ch.Name} [{status} | Power: {ch.Power} | Res: {ch.Resistance:P0}]");
            }
        }
        else
        {
            var ch = FindCharacter(idOrName);
            if (ch == null)
            {
                _displayer.Display($"Character '{idOrName}' not found.");
            }
            else
            {
                _displayer.Display(new CharacterDto(ch));
            }
        }
    }

    public void ListItems(string? idOrName = null)
    {
        if (string.IsNullOrWhiteSpace(idOrName))
        {
            _displayer.Display("=== Items Catalog ===");
            if (_items.Count == 0)
            {
                _displayer.Display("  (No items registered)");
                return;
            }

            foreach (var item in _items)
            {
                var users = _characters.Where(c => c.ActiveItems.Any(i => i.Id == item.Id)).Select(c => c.Name);
                _displayer.Display(new ItemDto(item, users));
            }
        }
        else
        {
            var item = FindItem(idOrName);
            if (item == null)
            {
                _displayer.Display($"Item '{idOrName}' not found.");
            }
            else
            {
                var users = _characters.Where(c => c.ActiveItems.Any(i => i.Id == item.Id)).Select(c => c.Name);
                _displayer.Display(new ItemDto(item, users));
            }
        }
    }

    public void ListAbilities(string? idOrName = null)
    {
        if (string.IsNullOrWhiteSpace(idOrName))
        {
            _displayer.Display("=== Abilities Grimoire ===");
            if (_abilities.Count == 0)
            {
                _displayer.Display("  (No abilities registered)");
                return;
            }

            foreach (var ab in _abilities)
            {
                var users = _characters.Where(c => c.AvailableAbilities.Any(a => a.Id == ab.Id)).Select(c => c.Name);
                _displayer.Display(new AbilityDto(ab, users));
            }
        }
        else
        {
            var ab = FindAbility(idOrName);
            if (ab == null)
            {
                _displayer.Display($"Ability '{idOrName}' not found.");
            }
            else
            {
                var users = _characters.Where(c => c.AvailableAbilities.Any(a => a.Id == ab.Id)).Select(c => c.Name);
                _displayer.Display(new AbilityDto(ab, users));
            }
        }
    }

    public Character? FindCharacter(string idOrName)
    {
        return _characters.FirstOrDefault(c =>
            c.Id.ToString() == idOrName || c.Name.Equals(idOrName, StringComparison.OrdinalIgnoreCase));
    }

    public Item? FindItem(string idOrName)
    {
        return _items.FirstOrDefault(i =>
            i.Id.ToString() == idOrName || i.Name.Equals(idOrName, StringComparison.OrdinalIgnoreCase));
    }

    public Ability? FindAbility(string idOrName)
    {
        return _abilities.FirstOrDefault(a =>
            a.Id.ToString() == idOrName || a.Name.Equals(idOrName, StringComparison.OrdinalIgnoreCase));
    }

    public CharacterSave ExportSave(string saveName)
    {
        var save = new CharacterSave
        {
            SaveName = saveName,
            LastModified = DateTime.UtcNow,
            Characters = _characters.Select(c => new CharacterData(
                c.Id,
                c.Name,
                c.CurrentHealth,
                c.Power,
                c.BaseArmor,
                c.BaseSpeed,
                c.Resistance,
                c.BaseResistPenetration,
                c.StatusEffects.Select(s => new StatusEffectData(
                    s.Name,
                    s.Type.ToString(),
                    s.DurationTurns,
                    s.PowerBonus,
                    s.ArmorBonus,
                    s.SpeedBonus,
                    s.ResistBonus,
                    s.DotDamage)).ToList()
            )).ToList(),
            Items = _items.Select(i => new ItemData(i.Id, i.Name, i.HpMod, i.PowerMod, i.ArmorMod, i.SpeedMod)).ToList(),
            Abilities = _abilities.Select(a => new AbilityData(a.Id, a.Name, a.Damage, a.Heal, a.Description, a.IsMagic, a.ResistPenetration, a.BonusProficiency)).ToList(),
            CharacterItems = [],
            CharacterAbilities = []
        };

        var joinItemId = 1;
        foreach (var c in _characters)
        {
            foreach (var it in c.ActiveItems)
            {
                save.CharacterItems.Add(new CharacterItems(joinItemId++, c.Id, it.Id));
            }
        }

        var joinAbilityId = 1;
        foreach (var c in _characters)
        {
            foreach (var ab in c.AvailableAbilities)
            {
                save.CharacterAbilities.Add(new CharacterAbilities(joinAbilityId++, c.Id, ab.Id));
            }
        }

        return save;
    }

    public void ImportSave(CharacterSave save)
    {
        _characters.Clear();
        _items.Clear();
        _abilities.Clear();
        _mediator.ClearCharacters();

        if (save.Items != null)
        {
            foreach (var idata in save.Items)
            {
                _items.Add(new Item(idata.Id, idata.Name, idata.HpMod, idata.PowerMod, idata.ArmorMod, idata.SpeedMod));
            }
        }

        if (save.Abilities != null)
        {
            foreach (var adata in save.Abilities)
            {
                _abilities.Add(new Ability(adata.Id, adata.Name, adata.Damage, adata.Heal, adata.Description, adata.IsMagic, adata.ResistPenetration, adata.BonusProficiency));
            }
        }

        if (save.Characters != null)
        {
            foreach (var cdata in save.Characters)
            {
                var ch = new Character(
                    cdata.Id,
                    cdata.Name,
                    (int)cdata.Hp,
                    (int)cdata.Power,
                    cdata.Resistance,
                    (int)cdata.Armor,
                    (int)cdata.Speed,
                    cdata.ResistPenetration);

                ch.SetCurrentHealth((int)cdata.Hp);

                if (cdata.StatusEffects != null)
                {
                    foreach (var se in cdata.StatusEffects)
                    {
                        if (Enum.TryParse<StatusEffectType>(se.Type, out var stype))
                        {
                            ch.AddStatusEffect(new StatusEffect(
                                se.Name,
                                stype,
                                se.DurationTurns,
                                se.PowerBonus,
                                se.ArmorBonus,
                                se.SpeedBonus,
                                se.ResistBonus,
                                se.DotDamage));
                        }
                    }
                }

                _characters.Add(ch);
                _mediator.RegisterCharacter(ch);
            }
        }

        if (save.CharacterItems != null)
        {
            foreach (var ci in save.CharacterItems)
            {
                var ch = _characters.FirstOrDefault(c => c.Id == ci.CharacterId);
                var item = _items.FirstOrDefault(i => i.Id == ci.ItemId);
                if (ch != null && item != null)
                {
                    ch.AddItem(item);
                }
            }
        }

        if (save.CharacterAbilities != null)
        {
            foreach (var ca in save.CharacterAbilities)
            {
                var ch = _characters.FirstOrDefault(c => c.Id == ca.CharacterId);
                var ability = _abilities.FirstOrDefault(a => a.Id == ca.AbilityId);
                if (ch != null && ability != null)
                {
                    ch.AddAbility(ability);
                }
            }
        }

        _nextCharId = _characters.Count > 0 ? _characters.Max(c => c.Id) + 1 : 1;
        _nextItemId = _items.Count > 0 ? _items.Max(i => i.Id) + 1 : 1;
        _nextAbilityId = _abilities.Count > 0 ? _abilities.Max(a => a.Id) + 1 : 1;

        _displayer.Display($"Successfully restored game state '{save.SaveName}' (Characters: {_characters.Count}, Items: {_items.Count}, Abilities: {_abilities.Count}).");
    }
}
